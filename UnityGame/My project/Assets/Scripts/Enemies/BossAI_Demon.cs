using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class BossAI_Demon : MonoBehaviour
{
    // -------------------- Inspector --------------------
    [Header("Refs")]
    public Transform player;
    public Animator animator;
    public Transform visual;

    [Header("Checks")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.25f;
    public LayerMask wallLayer;

    public Transform groundCheck;
    public float groundCheckRadius = 0.12f;
    public LayerMask groundLayer;

    [Header("Animator Params")]
    public string speedParam = "Speed";
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";
    public string deadBool = "IsDead";

    // Triggers de ataques (pon los nombres que tengas en tu Animator)
    public string meleeTrigger = "Melee";
    public string rangedTrigger = "Ranged";
    public string jumpTrigger = "JumpAttack";

    [Header("Flip")]
    public bool flipWithDirection = true;

    [Header("Movimiento")]
    public float patrolSpeed = 1.3f;
    public float chaseSpeed = 2.8f;
    public float patrolDistance = 2.5f;

    [Header("Detección")]
    public float visionRange = 9f;

    [Header("Melee")]
    public Transform meleePoint;
    public Vector2 meleeBoxSize = new Vector2(1.2f, 0.9f);
    public LayerMask playerLayer;
    public int meleeDamage = 1;
    public float meleeRange = 1.7f;
    public float meleeCooldown = 1.2f;

    [Header("Ranged (Fireball)")]
    public Transform shootPoint;
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public int projectileDamage = 1;
    public float rangedMinDistance = 2.5f;
    public float rangedMaxDistance = 6.5f;
    public float rangedCooldown = 1.6f;

    [Header("Jump Attack (opcional)")]
    public bool enableJumpAttack = true;
    public float jumpMinDistance = 3.0f;
    public float jumpMaxDistance = 5.5f;
    public float jumpCooldown = 3.0f;
    public float jumpUpForce = 9f;
    public float jumpForwardForce = 6f;

    [Header("Hurt/Stun")]
    public float hurtStunTime = 0.25f;

    [Header("Muerte")]
    public float deathDisableDelay = 2.0f;

    [Header("Boss Death Event")]
    public UnityEvent onBossDied;

    [Header("Sonidos")]
    public AudioSource meleeSFX;
    public AudioSource shootSFX;
    public AudioSource hurtSFX;
    public AudioSource deathSFX;

    // -------------------- Runtime --------------------
    enum State { Patrol, CombatIdle, Chase, Melee, Ranged, JumpAttack, Stunned, Dead }
    State state = State.Patrol;

    Rigidbody2D rb;
    RigidbodyConstraints2D baseConstraints;

    Vector2 patrolStart;
    int dir = 1;

    float meleeCd;
    float rangedCd;
    float jumpCd;

    Coroutine stunRoutine;
    Coroutine deathRoutine;

    Vector3 visualStartLocalPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseConstraints = rb.constraints;
        patrolStart = rb.position;

        if (visual == null)
        {
            var v = transform.Find("Visual");
            if (v != null) visual = v;
        }
        if (visual != null) visualStartLocalPos = visual.localPosition;

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

        if (meleePoint == null)
        {
            var mp = transform.Find("MeleePoint");
            if (mp != null) meleePoint = mp;
        }

        if (groundCheck == null)
        {
            var gc = transform.Find("GroundCheck");
            if (gc != null) groundCheck = gc;
        }
    }

    void Start()
    {
        AcquirePlayer();
    }

    void FixedUpdate()
    {
        if (state == State.Dead) return;

        if (player == null) AcquirePlayer();

        // CDs
        if (meleeCd > 0f) meleeCd -= Time.fixedDeltaTime;
        if (rangedCd > 0f) rangedCd -= Time.fixedDeltaTime;
        if (jumpCd > 0f) jumpCd -= Time.fixedDeltaTime;

        float dist = 999f;
        bool sees = false;

        if (player != null)
        {
            dist = Vector2.Distance(rb.position, player.position);
            sees = dist <= visionRange;
        }

        // Si no ve -> patrulla (salvo bloqueados)
        if ((state == State.CombatIdle || state == State.Chase) && !sees)
            EnterState(State.Patrol);

        if (state == State.Patrol && sees)
            EnterState(State.CombatIdle);

        switch (state)
        {
            case State.Patrol:
                DoPatrol();
                break;

            case State.CombatIdle:
                DoCombatIdle(dist, sees);
                break;

            case State.Chase:
                DoChase(dist);
                break;

            case State.Melee:
            case State.Ranged:
            case State.JumpAttack:
            case State.Stunned:
                // En estos estados el control real lo llevan las anims + Animation Events.
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
        }

        UpdateAnimator();
    }

    // -------------------- Estados --------------------
    void EnterState(State newState)
    {
        if (state == newState) return;

        // on-exit
        if (state == State.Melee || state == State.Ranged || state == State.JumpAttack)
            UnlockMovement();

        state = newState;

        switch (state)
        {
            case State.Patrol:
                break;

            case State.CombatIdle:
                rb.linearVelocity = Vector2.zero;
                FacePlayer();
                break;

            case State.Chase:
                break;

            case State.Melee:
                FacePlayer();
                LockMovementX();
                meleeCd = meleeCooldown;
                if (animator != null && !string.IsNullOrEmpty(meleeTrigger))
                    animator.SetTrigger(meleeTrigger);
                if (meleeSFX != null) meleeSFX.Play();
                break;

            case State.Ranged:
                FacePlayer();
                LockMovementX();
                rangedCd = rangedCooldown;
                if (animator != null && !string.IsNullOrEmpty(rangedTrigger))
                    animator.SetTrigger(rangedTrigger);
                break;

            case State.JumpAttack:
                FacePlayer();
                LockMovementX();
                jumpCd = jumpCooldown;
                if (animator != null && !string.IsNullOrEmpty(jumpTrigger))
                    animator.SetTrigger(jumpTrigger);
                break;

            case State.Stunned:
                rb.linearVelocity = Vector2.zero;
                break;

            case State.Dead:
                LockMovementX();
                break;
        }
    }

    void DoPatrol()
    {
        if (IsHittingWall(dir))
            TurnAround();

        rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);

        float dx = rb.position.x - patrolStart.x;
        if (Mathf.Abs(dx) >= patrolDistance)
            TurnAround();
    }

    void DoCombatIdle(float dist, bool sees)
    {
        rb.linearVelocity = Vector2.zero;

        if (!sees || player == null)
        {
            EnterState(State.Patrol);
            return;
        }

        FacePlayer();

        // Prioridad de ataques:
        // 1) Melee si está cerca
        if (dist <= meleeRange && meleeCd <= 0f)
        {
            EnterState(State.Melee);
            return;
        }

        // 2) Jump Attack si está a media distancia (opcional)
        if (enableJumpAttack && IsGrounded() && dist >= jumpMinDistance && dist <= jumpMaxDistance && jumpCd <= 0f)
        {
            EnterState(State.JumpAttack);
            return;
        }

        // 3) Ranged si está en rango
        if (dist >= rangedMinDistance && dist <= rangedMaxDistance && rangedCd <= 0f)
        {
            EnterState(State.Ranged);
            return;
        }

        // Si está demasiado lejos -> chase
        if (dist > meleeRange + 0.4f)
        {
            EnterState(State.Chase);
            return;
        }
    }

    void DoChase(float dist)
    {
        if (player == null) { EnterState(State.Patrol); return; }

        FacePlayer();

        if (IsHittingWall(dir))
            TurnAround();

        if (dist <= rangedMaxDistance)
        {
            EnterState(State.CombatIdle);
            return;
        }

        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
    }

    // -------------------- Animation Events --------------------
    // Llamar desde el frame del golpe
    public void AE_MeleeHit()
    {
        if (state != State.Melee) return;
        if (meleePoint == null) return;

        Collider2D hit = Physics2D.OverlapBox(meleePoint.position, meleeBoxSize, 0f, playerLayer);
        if (hit != null)
        {
            // Si tu Player tiene PlayerHealth con TakeDamage, cámbialo si hace falta
            var ph = hit.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(meleeDamage);
        }
    }

    // Llamar desde el frame donde sale la bola de fuego
    public void AE_Fireball()
    {
        if (state != State.Ranged) return;
        if (projectilePrefab == null || shootPoint == null) return;

        FacePlayer();

        if (shootSFX != null) shootSFX.Play();

        GameObject b = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

        var rbB = b.GetComponent<Rigidbody2D>();
        if (rbB != null)
            rbB.linearVelocity = new Vector2(dir * projectileSpeed, 0f);

        var p = b.GetComponent<Projectile>();
        if (p != null)
        {
            p.damage = projectileDamage;
            p.shooterTag = "Enemy";
        }
    }

    // Llamar desde la anim al “despegar”
    public void AE_JumpImpulse()
    {
        if (state != State.JumpAttack) return;
        if (!IsGrounded()) return;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float forward = dir * jumpForwardForce;
        rb.AddForce(new Vector2(forward, jumpUpForce), ForceMode2D.Impulse);
    }

    // Llamar desde la anim al final (para volver al combate)
    public void AE_AttackFinished()
    {
        if (state == State.Dead) return;
        EnterState(State.CombatIdle);
    }

    // -------------------- API para EnemyHealth (SIN TOCAR EnemyHealth) --------------------
    // EnemyHealth -> ai.PlayHurt()
    public void PlayHurt()
    {
        if (state == State.Dead) return;

        if (hurtSFX != null) hurtSFX.Play();

        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine());
    }

    // EnemyHealth -> ai.DieFromHealth()
    public void DieFromHealth()
    {
        DieBoss();
    }

    IEnumerator StunRoutine()
    {
        EnterState(State.Stunned);
        yield return new WaitForSeconds(hurtStunTime);

        bool sees = player != null && Vector2.Distance(rb.position, player.position) <= visionRange;
        EnterState(sees ? State.CombatIdle : State.Patrol);
    }

    void DieBoss()
    {
        if (state == State.Dead) return;
        state = State.Dead;

        if (deathSFX != null) deathSFX.Play();

        // Importante: cortar movimiento ya
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // animator flags
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deadBool))
                animator.SetBool(deadBool, true);

            if (!string.IsNullOrEmpty(dieTrigger))
                animator.SetTrigger(dieTrigger);
        }

        // ✅ Aquí está el EVENTO de muerte del boss (SIN tocar EnemyHealth)
        onBossDied?.Invoke();

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

    void LockMovementX()
    {
        rb.constraints = baseConstraints | RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void UnlockMovement()
    {
        rb.constraints = baseConstraints;
    }

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

    bool IsGrounded()
    {
        if (groundCheck == null) return true; // si no hay, no bloquees
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float spd = Mathf.Abs(rb.linearVelocity.x);
        if (state == State.Melee || state == State.Ranged || state == State.JumpAttack || state == State.Stunned || state == State.Dead)
            spd = 0f;

        animator.SetFloat(speedParam, spd);
    }

    // Gizmos para ver melee box
    void OnDrawGizmosSelected()
    {
        if (meleePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(meleePoint.position, meleeBoxSize);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}