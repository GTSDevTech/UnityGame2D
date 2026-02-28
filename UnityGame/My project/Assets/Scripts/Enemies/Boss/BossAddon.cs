using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class BossAddon : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyAI_Shooter baseAI;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Animator Triggers (exactos)")]
    public string slamTrigger = "Slam";     // tu trigger real
    public string attackTrigger = "Attack"; // tu trigger real (para DemonAttackAnimation)

    [Header("Slam (Jump + Fall)")]
    public float slamCooldown = 4.5f;
    public float jumpUpVelocity = 12f;
    public float jumpHorizontalVelocity = 5f;

    [Header("Landing detection (robusto anti-corte)")]
    public float minAirTime = 0.20f;            // evita grounded instantáneo
    public float fallingVelThreshold = 0.05f;   // espera a estar cayendo
    public int groundedStableFrames = 3;        // grounded N frames seguidos
    public float landFreezeTime = 0.10f;        // pausa de impacto

    [Header("AOE 360º (Slam + DemonAttack)")]
    public float aoeRadius = 2.5f;
    public LayerMask playerLayer;
    public int aoeDamage = 1;

    [Header("Phase 2 (<= 50% vida)")]
    public bool phase2Enabled = true;
    public float phase2Threshold = 0.5f;
    public float phase2CooldownMultiplier = 0.75f;
    public int phase2AoeBonusDamage = 1;

    // Runtime
    public bool InSlam => inSlam;

    private bool inSlam;
    private bool phase2;
    private float slamTimer;
    private RigidbodyConstraints2D savedConstraints;

    private bool hasSlamTrigger;
    private bool hasAttackTrigger;

    void Awake()
    {
        if (!baseAI) baseAI = GetComponent<EnemyAI_Shooter>();
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

        savedConstraints = rb.constraints;

        // Cache de triggers existentes (evita "Parameter does not exist")
        if (animator)
        {
            hasSlamTrigger = HasParam(slamTrigger, AnimatorControllerParameterType.Trigger);
            hasAttackTrigger = HasParam(attackTrigger, AnimatorControllerParameterType.Trigger);
        }

        slamTimer = Random.Range(0.3f, 0.8f);
    }

    void Update()
    {
        // cooldown interno del slam (lo consume RequestSlam)
        if (slamTimer > 0f) slamTimer -= Time.deltaTime;

        // fase 2 por vida (robusto)
        if (phase2Enabled && !phase2 && TryGetHealth01(out float h01) && h01 <= phase2Threshold)
            phase2 = true;
    }

    // =========================
    // API pública para el Director
    // =========================
    public bool CanSlam()
    {
        if (inSlam) return false;
        if (slamTimer > 0f) return false;
        if (TryIsDead(out bool dead) && dead) return false;
        return true;
    }

    public bool RequestSlam()
    {
        if (!CanSlam()) return false;
        StartCoroutine(SlamRoutine());
        return true;
    }

    // =========================
    // Anim Events (para DemonAttackAnimation)
    // Añade un Animation Event en el frame del impacto:
    //     Anim_DemonAttackAOE
    // =========================
    public void Anim_DemonAttackAOE()
    {
        DoAOE(aoeDamage);
    }

    // =========================
    // Slam
    // =========================
    IEnumerator SlamRoutine()
    {
        inSlam = true;

        float cd = slamCooldown * (phase2 ? phase2CooldownMultiplier : 1f);
        slamTimer = cd;

        // Evita interferencias si EnemyAI había congelado X por disparo/recarga
        savedConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Pausar IA base para que no pise la física del salto
        if (baseAI) baseAI.enabled = false;

        // Refrescar player por si se pierde
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        // Cara al player + trigger del slam (respeta tu controller Idle/Walk/Run -> Jump)
        if (player)
        {
            int dir = (player.position.x >= rb.position.x) ? 1 : -1;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            transform.localScale = s;
        }

        if (animator && hasSlamTrigger && !string.IsNullOrEmpty(slamTrigger))
            animator.SetTrigger(slamTrigger);

        // impulso
        float dirF = (player && player.position.x >= rb.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(dirF * jumpHorizontalVelocity, jumpUpVelocity);

        // 1) mínimo en aire
        float t = 0f;
        while (t < minAirTime) { t += Time.deltaTime; yield return null; }

        // 2) esperar a caer
        while (rb.linearVelocity.y > fallingVelThreshold)
            yield return null;

        // 3) grounded estable
        int g = 0;
        while (g < Mathf.Max(1, groundedStableFrames))
        {
            if (IsGroundedFromBase()) g++;
            else g = 0;

            yield return null;
        }

        // Forzar velocidad a 0 para no romper transiciones tipo Speed < 0.1
        rb.linearVelocity = Vector2.zero;

        // Impacto + AOE 360º
        DoAOE(aoeDamage);

        yield return new WaitForSeconds(landFreezeTime);

        // Volver a IA base
        if (baseAI) baseAI.enabled = true;
        rb.constraints = savedConstraints;

        inSlam = false;
    }

    // =========================
    // AOE 360º (misma para Slam y DemonAttack)
    // =========================
    void DoAOE(int baseDamage)
    {
        int dmg = baseDamage + (phase2 ? phase2AoeBonusDamage : 0);

        var hits = Physics2D.OverlapCircleAll(rb.position, aoeRadius, playerLayer);
        if (hits == null || hits.Length == 0) return;

        // Aplica UNA VEZ aunque haya varios colliders del Player
        PlayerHealth ph = null;
        for (int i = 0; i < hits.Length; i++)
        {
            ph = hits[i].GetComponentInParent<PlayerHealth>();
            if (ph) break;
        }

        if (ph) ph.TakeDamage(dmg);
    }

    // =========================
    // Ground check (usa el del EnemyAI_Shooter)
    // =========================
    bool IsGroundedFromBase()
    {
        if (!baseAI || !baseAI.groundCheck) return false;
        return Physics2D.OverlapCircle(baseAI.groundCheck.position, baseAI.groundCheckRadius, baseAI.groundLayer) != null;
    }

    // =========================
    // Helpers (robustos para EnemyHealth)
    // =========================
    bool TryGetHealth01(out float h01)
    {
        h01 = 1f;
        if (!health) return false;

        // Si tu EnemyHealth ya tiene Health01 (ideal)
        var prop = health.GetType().GetProperty("Health01");
        if (prop != null && prop.PropertyType == typeof(float))
        {
            h01 = (float)prop.GetValue(health);
            return true;
        }

        // Fallback: intenta currentHealth/maxHealth (campos típicos)
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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
#endif
}