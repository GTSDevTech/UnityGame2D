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

    [Header("Ground Check (como Player, pero automático)")]
    [Tooltip("Si no lo asignas, buscará un hijo llamado GroundCheck.")]
    public Transform groundCheck;
    [Tooltip("Radio del overlap para detectar suelo.")]
    public float groundCheckRadius = 0.12f;
    [Tooltip("LayerMask del suelo (Ground).")]
    public LayerMask groundLayer;

    [Header("Animator Params (exactos)")]
    public string speedParam = "Speed";

    // Controller viejo (Enemy): bool
    public string shootBool = "IsShooting";

    // Controller Player: trigger
    public string shootTrigger = "Shoot";

    // Controller Player: grounded bool
    public string groundedBool = "isGrounded";

    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";
    public string deadBool = "IsDead";
    public string reloadTrigger = "Reload";

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

    [Header("Sonidos")]
    public AudioSource shootSFX;
    public AudioSource hurtSFX;
    public AudioSource reloadSFX;
    public AudioSource deathSFX;

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

    // Cache de params del Animator (para no romper si el controller no los tiene)
    bool _hasShootBool;
    bool _hasShootTrigger;
    bool _hasGroundedBool;
    bool _hasSpeedFloat;

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

        // Auto WallCheck
        if (wallCheck == null)
        {
            var wc = transform.Find("WallCheck");
            if (wc != null) wallCheck = wc;
        }

        // Auto GroundCheck
        if (groundCheck == null)
        {
            var gc = transform.Find("GroundCheck");
            if (gc != null) groundCheck = gc;
        }

        // Auto ShootPoint
        if (shootPoint == null)
        {
            var sp = transform.Find("ShootPoint");
            if (sp != null) shootPoint = sp;
        }

        // Cache params del animator (IMPORTANTE: evita el error "Parameter does not exist")
        CacheAnimatorParams();
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

        // actualizar bool de disparo solo cuando estés en Shoot (solo si existe en el controller)
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

                // NUEVO: si el controller es el del Player, dispara por Trigger "Shoot"
                if (animator != null && _hasShootTrigger && !string.IsNullOrEmpty(shootTrigger))
                    animator.SetTrigger(shootTrigger);

                break;

            case State.Reload:
                LockMovement();
                reloadTimer = reloadDuration;

                if (reloadSFX != null) reloadSFX.Play(); // 🔊 RECARGA

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
    // -------------------- Proyectiles --------------------
// Animation Event
    public void FireProjectile()
    {
        // IMPORTANTE: No dependas del state exacto del script para permitir el AnimEvent.
        // Si ya estás muerto / stunned / recargando, no dispares.
        if (state == State.Dead || state == State.Stunned || state == State.Reload) return;

        if (projectilePrefab == null || shootPoint == null) return;

        if (shootSFX != null) shootSFX.Play();

        // No recalcules FacePlayer aquí: puede cambiarte dir justo en el frame del disparo.
        // La dirección debe venir del "dir" ya establecido por la IA/estado.
        int shootDir = dir;
        if (shootDir == 0) shootDir = 1;

        // Spawn (tal cual, en el shootPoint)
        GameObject b = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

        // Layer a toda la jerarquía del proyectil
        if (enemyProjectileLayer >= 0)
        {
            b.layer = enemyProjectileLayer;
            foreach (Transform t in b.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = enemyProjectileLayer;
        }

        // (Recomendado) Ignorar colisión con el enemigo (por si hay overlaps con colliders hijos)
        Collider2D enemyCol = GetComponent<Collider2D>();
        Collider2D bulletCol = b.GetComponent<Collider2D>();
        if (enemyCol != null && bulletCol != null)
            Physics2D.IgnoreCollision(enemyCol, bulletCol);

        // Velocidad
        Rigidbody2D rbB = b.GetComponent<Rigidbody2D>();
        if (rbB != null)
            rbB.linearVelocity = new Vector2(shootDir * projectileSpeed, 0f);

        // Datos de daño/autor
        Projectile p = b.GetComponent<Projectile>();
        if (p != null)
        {
            p.damage = projectileDamage;
            p.shooterTag = gameObject.tag; // correcto
        }

        // Contador de disparos/recarga
        shotsSinceReload++;
        if (shotsSinceReload >= shotsBeforeReload)
            pendingReload = true;
    }

    // -------------------- API para EnemyHealth (Opción A) --------------------
    public void PlayHurt()
    {
        if (state == State.Dead) return;

        if (hurtSFX != null) hurtSFX.Play(); // 🔊 HURT

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
        if (deathSFX != null) deathSFX.Play();
        if (state == State.Dead) return;

        state = State.Dead;
        pendingReload = false;

        // CLAVE: si muere disparando, el bool puede quedarse "true" si el controller lo tiene
        if (animator != null && _hasShootBool && !string.IsNullOrEmpty(shootBool))
            animator.SetBool(shootBool, false);

        UnlockMovement();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (visual != null)
            visual.localPosition = visualStartLocalPos + new Vector3(0f, deathVisualYOffset, 0f);

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deadBool))
                animator.SetBool(deadBool, true);

            if (!string.IsNullOrEmpty(dieTrigger))
                animator.SetTrigger(dieTrigger);
        }

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

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
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

        if (_hasSpeedFloat && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, spd);

        // NUEVO: grounded real para controller del Player (evita "saltando")
        if (_hasGroundedBool && !string.IsNullOrEmpty(groundedBool))
            animator.SetBool(groundedBool, IsGrounded());
    }

    void SetShootingBool(bool value)
    {
        if (animator == null || string.IsNullOrEmpty(shootBool)) return;
        if (!_hasShootBool) return; // evita el error si el controller no tiene IsShooting
        animator.SetBool(shootBool, value);
    }

    void CacheAnimatorParams()
    {
        if (animator == null)
        {
            _hasShootBool = false;
            _hasShootTrigger = false;
            _hasGroundedBool = false;
            _hasSpeedFloat = false;
            return;
        }

        _hasShootBool = HasParam(shootBool, AnimatorControllerParameterType.Bool);
        _hasShootTrigger = HasParam(shootTrigger, AnimatorControllerParameterType.Trigger);
        _hasGroundedBool = HasParam(groundedBool, AnimatorControllerParameterType.Bool);
        _hasSpeedFloat = HasParam(speedParam, AnimatorControllerParameterType.Float);
    }

    bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == type) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
        }
    }
#endif
}