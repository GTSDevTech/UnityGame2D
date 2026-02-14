using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI_Shooter : MonoBehaviour
{
    // -------------------- Inspector --------------------
    [Header("Referencias")]
    public Transform player;

    [Tooltip("Animator (si lo tienes en un hijo Visual, asígnalo aquí o se auto-busca)")]
    public Animator animator;

    [Tooltip("Hijo que contiene Sprite/Animator (opcional). Si existe, se baja al morir para que no “flote”.")]
    public Transform visual;

    [Header("Checks")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.25f;
    public LayerMask wallLayer;

    [Header("Animator Params (exactos)")]
    public string speedParam = "Speed";
    public string shootBool = "IsShooting";
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";     // opcional
    public string deadBool = "IsDead";    // recomendado
    public string reloadTrigger = "Reload"; // Trigger para recarga (AnyState->Recharge con condición Reload)

    [Header("Hurt/Stun")]
    public float hurtStunTime = 0.25f;

    [Header("Disparo (proyectil)")]
    public Transform shootPoint;
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public int projectileDamage = 1;

    [Header("Velocidades")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float backOffSpeed = 2.0f;

    [Header("Detección")]
    public float visionRange = 7f;

    [Header("Distancias de combate")]
    public float stopDistance = 3.0f;
    public float tooCloseDistance = 2.2f;
    public float shootDistance = 3.5f;

    [Header("Disparo (estado)")]
    public float shootDuration = 0.6f;
    public float shootCooldown = 0.9f;

    [Header("Recarga")]
    public int shotsBeforeReload = 3;
    public float reloadDuration = 1.2f;     // variable de duración
    public float postReloadCooldown = 1.2f; // evita que dispare instant al acabar recarga

    [Header("Patrulla")]
    public float patrolDistance = 2f;

    [Header("Flip")]
    public bool flipWithDirection = true;

    [Header("Muerte")]
    public float deathDisableDelay = 1.5f;

    [Header("Fix visual muerte (si “flota”)")]
    public float deathVisualYOffset = -0f;
    Vector3 visualStartLocalPos;

    [Header("Projectile Layer (Physics)")]
    [Tooltip("Layer física para los proyectiles del enemigo (debe existir en Unity).")]
    public string enemyProjectileLayerName = "Projectile_Enemy";

    // -------------------- Runtime --------------------
    enum State { Patrol, CombatIdle, Chase, BackOff, Shoot, Reload, Stunned, Dead }
    State state = State.Patrol;

    Rigidbody2D rb;
    RigidbodyConstraints2D baseConstraints;

    Vector2 patrolStart;
    int dir = 1;

    int shotsSinceReload = 0;

    float shootTimer = 0f;
    float cooldownTimer = 0f;
    float reloadTimer = 0f;

    bool pendingReload = false;

    Coroutine deathRoutine;

    int enemyProjectileLayer = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseConstraints = rb.constraints;

        patrolStart = rb.position;

        // cache layer id (evita NameToLayer cada disparo)
        enemyProjectileLayer = LayerMask.NameToLayer(enemyProjectileLayerName);

        // Auto asignar Visual
        if (visual == null)
        {
            var v = transform.Find("Visual");
            if (v != null) visual = v;
        }
        if (visual != null)
            visualStartLocalPos = visual.localPosition;

        // Auto asignar Animator
        if (animator == null)
        {
            animator = (visual != null) ? visual.GetComponent<Animator>() : GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        if (wallCheck == null)
        {
            var wc = transform.Find("WallCheck");
            if (wc != null) wallCheck = wc;
        }

        if (shootPoint == null)
        {
            var sp = transform.Find("ShootPoint");
            if (sp != null) shootPoint = sp;
        }
    }

    void Start()
    {
        AcquirePlayer();

        if (tooCloseDistance > stopDistance) tooCloseDistance = stopDistance * 0.75f;
        if (shootDistance < stopDistance) shootDistance = stopDistance;
    }

    void FixedUpdate()
    {
        if (state == State.Dead) return;

        // refrescar player si se pierde (por respawn/escena)
        if (player == null) AcquirePlayer();

        // timers globales
        if (cooldownTimer > 0f) cooldownTimer -= Time.fixedDeltaTime;
        if (shootTimer > 0f) shootTimer -= Time.fixedDeltaTime;
        if (reloadTimer > 0f) reloadTimer -= Time.fixedDeltaTime;

        // actualizar bool de disparo solo cuando estés en Shoot
        SetShootingBool(state == State.Shoot);

        // decidir si ve al player
        bool sees = false;
        float dist = 999f;

        if (player != null)
        {
            dist = Vector2.Distance(rb.position, player.position);
            sees = dist <= visionRange;
        }

        // Transiciones “globales” por visión (excepto estados bloqueados)
        if (state == State.Patrol && sees)
            EnterState(State.CombatIdle);

        if ((state == State.CombatIdle || state == State.Chase || state == State.BackOff) && !sees)
            EnterState(State.Patrol);

        // ejecutar estado
        switch (state)
        {
            case State.Patrol:
                DoPatrol();
                break;

            case State.CombatIdle:
                DoCombatIdle(dist);
                break;

            case State.Chase:
                DoChase(dist);
                break;

            case State.BackOff:
                DoBackOff(dist);
                break;

            case State.Shoot:
                DoShootState();
                break;

            case State.Reload:
                DoReloadState();
                break;

            case State.Stunned:
                DoStunnedState();
                break;
        }

        UpdateAnimator();
    }

    // -------------------- Estados --------------------
    void EnterState(State newState)
    {
        if (state == newState) return;

        // on-exit
        if (state == State.Shoot || state == State.Reload)
            UnlockMovement();

        state = newState;

        switch (state)
        {
            case State.Patrol:
                break;

            case State.CombatIdle:
                rb.linearVelocity = Vector2.zero;
                break;

            case State.Shoot:
                FacePlayer();
                LockMovement();
                shootTimer = shootDuration;
                cooldownTimer = shootCooldown;
                break;

            case State.Reload:
                LockMovement();
                reloadTimer = reloadDuration;

                if (animator != null && !string.IsNullOrEmpty(reloadTrigger))
                    animator.SetTrigger(reloadTrigger);
                break;

            case State.Stunned:
                rb.linearVelocity = Vector2.zero;
                break;

            case State.Dead:
                LockMovement();
                break;
        }
    }

    void DoPatrol()
    {
        if (IsHittingWall())
            TurnAround();

        rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);

        float dx = rb.position.x - patrolStart.x;
        if (Mathf.Abs(dx) >= patrolDistance)
            TurnAround();
    }

    void DoCombatIdle(float dist)
    {
        rb.linearVelocity = Vector2.zero;
        FacePlayer();

        if (player == null) { EnterState(State.Patrol); return; }

        if (dist <= tooCloseDistance)
        {
            EnterState(State.BackOff);
            return;
        }

        if (dist > stopDistance)
        {
            EnterState(State.Chase);
            return;
        }

        if (dist <= shootDistance && cooldownTimer <= 0f)
        {
            if (shotsSinceReload >= shotsBeforeReload)
                EnterState(State.Reload);
            else
                EnterState(State.Shoot);

            return;
        }
    }

    void DoChase(float dist)
    {
        FacePlayer();

        if (IsHittingWall())
            TurnAround();

        if (dist <= stopDistance)
        {
            EnterState(State.CombatIdle);
            return;
        }

        rb.linearVelocity = new Vector2(dir * runSpeed, rb.linearVelocity.y);
    }

    void DoBackOff(float dist)
    {
        FacePlayer();

        int awayDir = (player != null && player.position.x >= rb.position.x) ? -1 : 1;

        if (IsHittingWall(awayDir))
            awayDir *= -1;

        rb.linearVelocity = new Vector2(awayDir * backOffSpeed, rb.linearVelocity.y);

        if (dist > tooCloseDistance + 0.2f)
            EnterState(State.CombatIdle);
    }

    void DoShootState()
    {
        LockMovement();

        if (shootTimer <= 0f)
        {
            if (pendingReload)
            {
                pendingReload = false;

                if (animator != null && !string.IsNullOrEmpty(reloadTrigger))
                    animator.SetTrigger(reloadTrigger);

                EnterState(State.Reload);
                return;
            }

            EnterState(State.CombatIdle);
        }
    }

    void DoReloadState()
    {
        LockMovement();

        if (reloadTimer <= 0f)
        {
            shotsSinceReload = 0;
            cooldownTimer = postReloadCooldown;
            EnterState(State.CombatIdle);
        }
    }

    void DoStunnedState()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // -------------------- Proyectiles --------------------
    // Animation Event
    public void FireProjectile()
    {
        if (state != State.Shoot) return;
        if (projectilePrefab == null || shootPoint == null) return;

        FacePlayer();

        GameObject b = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

        if (enemyProjectileLayer >= 0)
        {
            b.layer = enemyProjectileLayer;
            foreach (Transform t in b.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = enemyProjectileLayer;
        }

        var rbB = b.GetComponent<Rigidbody2D>();
        if (rbB != null)
            rbB.linearVelocity = new Vector2(dir * projectileSpeed, 0f);

        var p = b.GetComponent<Projectile>();
        if (p != null)
        {
            p.damage = projectileDamage;
            p.shooterTag = "Enemy";
        }

        shotsSinceReload++;

        if (shotsSinceReload >= shotsBeforeReload)
            pendingReload = true;
    }

    // -------------------- API para EnemyHealth (Opción A) --------------------
    public void PlayHurt()
    {
        if (state == State.Dead) return;

        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        StopAllCoroutines();
        StartCoroutine(HurtStunRoutine());
    }

    public void DieFromHealth()
    {
        Die();
    }

    IEnumerator HurtStunRoutine()
    {
        EnterState(State.Stunned);
        yield return new WaitForSeconds(hurtStunTime);

        bool sees = player != null && Vector2.Distance(rb.position, player.position) <= visionRange;
        EnterState(sees ? State.CombatIdle : State.Patrol);
    }

    void Die()
    {
        if (state == State.Dead) return;

        state = State.Dead;

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deadBool)) animator.SetBool(deadBool, true);
            if (!string.IsNullOrEmpty(dieTrigger)) animator.SetTrigger(dieTrigger);
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = baseConstraints | RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        if (visual != null)
            visual.localPosition = visualStartLocalPos + new Vector3(0f, deathVisualYOffset, 0f);

        if (deathRoutine != null) StopCoroutine(deathRoutine);
        deathRoutine = StartCoroutine(DisableAfterDeath());
    }

    IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(deathDisableDelay);
        this.enabled = false;
    }

    // -------------------- Helpers --------------------
    void AcquirePlayer()
    {
        if (player != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
    }

    void FacePlayer()
    {
        if (player == null) return;
        dir = (player.position.x >= rb.position.x) ? 1 : -1;
        ApplyFlip();
    }

    void LockMovement()
    {
        rb.constraints = baseConstraints | RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void UnlockMovement()
    {
        rb.constraints = baseConstraints;
    }

    bool IsHittingWall() => IsHittingWall(dir);

    bool IsHittingWall(int checkDir)
    {
        if (wallCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * checkDir, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    void TurnAround()
    {
        dir *= -1;
        patrolStart = rb.position;
        ApplyFlip();
    }

    void ApplyFlip()
    {
        if (!flipWithDirection) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float spd = Mathf.Abs(rb.linearVelocity.x);

        if (state == State.Shoot || state == State.Reload || state == State.Stunned || state == State.Dead)
            spd = 0f;

        animator.SetFloat(speedParam, spd);
    }

    void SetShootingBool(bool value)
    {
        if (animator == null || string.IsNullOrEmpty(shootBool)) return;
        animator.SetBool(shootBool, value);
    }
}
