using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class BossAttackDirector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyAI_Shooter enemyAI; // shoot normal (lo dejamos vivir)
    [SerializeField] private BossAddon bossAddon;     // slam + aoe + anim event aoe
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Animator Triggers (exactos)")]
    public string attackTrigger = "Attack"; // DemonAttackAnimation
    public string slamTrigger = "Slam";     // DemonJumpAnimation

    [Header("Nombres de ESTADOS (exactos en tu controller)")]
    public string stateIdle = "Idle";
    public string stateWalk = "DemonWalkAnimation";
    public string stateRun  = "DemonRunAnimation";

    public string stateAttack = "DemonAttackAnimation";
    public string stateJump   = "DemonJumpAnimation";

    [Header("Decisión")]
    public float decisionInterval = 0.25f;

    [Header("Rangos")]
    public float attackRange = 2.5f;         // muy cerca => Attack (AOE 360º)
    public float slamRange = 6.5f;           // medio => Slam
    public float minPlayerHeightDeltaToSlam = 1.2f; // si está bastante más alto => Slam “para seguir”

    [Header("Cooldowns")]
    public float attackCooldown = 2.8f;
    public float slamCooldownExtra = 0f; // extra por encima del cooldown del BossAddon, si quieres

    [Header("Fase 2 (<= 50% vida)")]
    public bool phase2Enabled = true;
    public float phase2Threshold = 0.5f;
    public float phase2CooldownMultiplier = 0.8f;
    [Range(0f, 1f)] public float phase2AttackBias = 0.65f; // probabilidades extra
    [Range(0f, 1f)] public float phase2SlamBias = 0.55f;

    private float nextDecision;
    private float attackTimer;
    private float slamTimer;
    private bool phase2;
    private bool locked; // ejecutando ataque (para no spamear triggers)

    private bool hasAttackTrigger;
    private bool hasSlamTrigger;

    void Awake()
    {
        if (!enemyAI) enemyAI = GetComponent<EnemyAI_Shooter>();
        if (!bossAddon) bossAddon = GetComponent<BossAddon>();
        if (!health) health = GetComponent<EnemyHealth>();
        if (!rb) rb = GetComponent<Rigidbody2D>();

        if (!animator)
        {
            var vis = transform.Find("Visual");
            if (vis) animator = vis.GetComponent<Animator>();
            if (!animator) animator = GetComponentInChildren<Animator>();
        }

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        nextDecision = Time.time + Random.Range(0.05f, 0.2f);

        if (animator)
        {
            hasAttackTrigger = HasParam(attackTrigger, AnimatorControllerParameterType.Trigger);
            hasSlamTrigger = HasParam(slamTrigger, AnimatorControllerParameterType.Trigger);
        }
    }

    void Update()
    {
        if (!rb || !animator || !enemyAI || !bossAddon || !health) return;
        if (TryIsDead(out bool dead) && dead) return;

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }
        if (!player) return;

        if (phase2Enabled && !phase2 && TryGetHealth01(out float h01) && h01 <= phase2Threshold)
            phase2 = true;

        if (locked) return;

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (slamTimer > 0f) slamTimer -= Time.deltaTime;

        if (Time.time < nextDecision) return;
        nextDecision = Time.time + decisionInterval;

        // Solo decide si estás en locomoción (Idle/Walk/Run) para respetar tu grafo
        if (!IsInLocomotion()) return;

        float dist = Vector2.Distance(rb.position, player.position);
        float dy = player.position.y - rb.position.y;

        // 1) MUY CERCA => Attack AOE (360º)
        if (dist <= attackRange && attackTimer <= 0f && hasAttackTrigger)
        {
            StartCoroutine(DoAttack());
            return;
        }

        // 2) MEDIO o player bastante más alto => Slam
        bool wantSlamByHeight = dy >= minPlayerHeightDeltaToSlam;
        bool wantSlamByDist = dist <= slamRange;

        if ((wantSlamByDist || wantSlamByHeight) && slamTimer <= 0f)
        {
            float bias = phase2 ? phase2SlamBias : 0.35f;
            if (Random.value <= bias)
            {
                StartCoroutine(DoSlam());
                return;
            }
        }

        // 3) Lejos => NO hacemos nada: EnemyAI_Shooter dispara normal con su propia lógica
        // (Si quieres, aquí podrías ajustar valores del enemyAI SOLO en boss, pero no hace falta)
        
        // Extra fase2: probabilidad de Attack incluso fuera de rango cercano (opcional y suave)
        if (phase2 && dist <= slamRange && attackTimer <= 0f && hasAttackTrigger)
        {
            if (Random.value <= phase2AttackBias * 0.2f)
                StartCoroutine(DoAttack());
        }
    }

    IEnumerator DoAttack()
    {
        locked = true;

        float cd = attackCooldown * (phase2 ? phase2CooldownMultiplier : 1f);
        attackTimer = cd;

        FacePlayer();

        animator.SetTrigger(attackTrigger);

        // Espera a que realmente entre en DemonAttackAnimation (respetando que viene desde Idle/Walk/Run)
        yield return new WaitUntil(() => IsInState(stateAttack));

        // Pausa EnemyAI para que no dispare/mueva durante la anim
        enemyAI.enabled = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Espera a salir del estado de ataque (vuelve a locomoción)
        yield return new WaitUntil(IsInLocomotion);

        enemyAI.enabled = true;
        locked = false;
    }

    IEnumerator DoSlam()
    {
        locked = true;

        float cd = (bossAddon.slamCooldown + slamCooldownExtra) * (phase2 ? phase2CooldownMultiplier : 1f);
        slamTimer = cd;

        FacePlayer();

        // Lo más limpio: pedir al BossAddon que ejecute el slam (y él dispara trigger Slam si existe)
        // Pero también lanzamos el trigger aquí por seguridad (no rompe nada si se duplica).
        if (hasSlamTrigger) animator.SetTrigger(slamTrigger);

        bossAddon.RequestSlam();

        // Espera a que entre en estado de salto (si tu controller lo hace)
        yield return new WaitUntil(() => IsInState(stateJump) || bossAddon.InSlam);

        // Espera a que termine y vuelva a locomoción
        yield return new WaitUntil(() => IsInLocomotion() && !bossAddon.InSlam);

        locked = false;
    }

    void FacePlayer()
    {
        int dir = (player.position.x >= rb.position.x) ? 1 : -1;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    bool IsInLocomotion()
    {
        var st = animator.GetCurrentAnimatorStateInfo(0);
        return st.IsName(stateIdle) || st.IsName(stateWalk) || st.IsName(stateRun);
    }

    bool IsInState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return false;
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    // ===== Helpers robustos para EnemyHealth =====
    bool TryGetHealth01(out float h01)
    {
        h01 = 1f;
        if (!health) return false;

        var prop = health.GetType().GetProperty("Health01");
        if (prop != null && prop.PropertyType == typeof(float))
        {
            h01 = (float)prop.GetValue(health);
            return true;
        }

        var fCur = health.GetType().GetField("currentHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        var fMax = health.GetType().GetField("maxHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        if (fCur != null && fMax != null)
        {
            int cur = (int)fCur.GetValue(health);
            int max = (int)fMax.GetValue(health);
            h01 = max <= 0 ? 0f : Mathf.Clamp01((float)cur / max);
            return true;
        }

        return false;
    }

    bool TryIsDead(out bool dead)
    {
        dead = false;
        if (!health) return false;

        var prop = health.GetType().GetProperty("IsDead");
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            dead = (bool)prop.GetValue(health);
            return true;
        }

        var f = health.GetType().GetField("isDead", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f != null && f.FieldType == typeof(bool))
        {
            dead = (bool)f.GetValue(health);
            return true;
        }

        return false;
    }

    bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (!animator || string.IsNullOrEmpty(name)) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}