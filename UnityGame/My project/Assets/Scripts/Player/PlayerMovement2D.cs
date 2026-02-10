using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Movimiento Horizontal")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;

    [Header("Salto")]
    public float jumpForce = 12f;
    public bool resetVerticalVelocityBeforeJump = true;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Orientación")]
    public bool flipWithDirection = true;

    [Header("Animator Params")]
    public string speedParam = "Speed";
    public string groundedParam = "isGrounded";
    public string runParam = "Run";
    public string attackTrigger = "Attack";
    public string shootTrigger = "Shoot";
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";
    public string deadBool = "IsDead";

    [Header("Ataque melee")]
    public float attackDuration = 0.35f;
    public bool freezeMovementWhileAttacking = true;

    [Header("Shoot")]
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 12f;
    public float shootCooldown = 0.25f;
    public int bulletDamage = 1;

    [Tooltip("Tiempo que el player queda inmóvil al disparar. Si tu animación dura más/menos, ajusta esto.")]
    public float shootLockDuration = 0.15f;

    [Header("Lock de movimiento (general)")]
    [Tooltip("Si está activado, además de velocidad 0, congela X con constraints durante ataque/disparo.")]
    public bool freezeXWithConstraintsWhileLocked = true;

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("Debug")]
    public bool debugHUD = false;
    public bool debugLogs = false;

    [Header("Muerte (visual)")]
    public Transform visual;
    public float deathVisualYOffset = -0.25f; // ajusta esto

    Rigidbody2D rb;
    Collider2D col;
    PlayerInput playerInput;

    InputAction moveAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction attackAction;
    InputAction shootAction;

    Vector2 moveInput;
    bool isRunning;
    bool isGrounded;
    bool isAttacking;
    bool isDead;
    bool canShoot = true;

    float coyoteCounter;
    float jumpBufferCounter;

    Vector3 visualStartLocalPos;

    Coroutine attackRoutine;

    // ---- MOVEMENT LOCK ----
    bool isMovementLocked = false;
    float movementLockTimer = 0f;
    bool lockFreezesX = false;
    RigidbodyConstraints2D baseConstraints;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();

        baseConstraints = rb.constraints; // normalmente FreezeRotation

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (shootPoint == null)
        {
            var sp = transform.Find("ShootPoint");
            if (sp != null) shootPoint = sp;
        }

        if (visual == null)
        {
            var v = transform.Find("Visual");
            if (v != null) visual = v;
        }

        if (visual != null)
            visualStartLocalPos = visual.localPosition;

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
        attackAction = playerInput.actions["Attack"];
        shootAction = playerInput.actions["Shoot"];
    }

    void OnEnable()
    {
        playerInput.currentActionMap?.Enable();
        moveAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        attackAction?.Enable();
        shootAction?.Enable();
    }

    void OnDisable()
    {
        shootAction?.Disable();
        attackAction?.Disable();
        sprintAction?.Disable();
        jumpAction?.Disable();
        moveAction?.Disable();
        playerInput.currentActionMap?.Disable();
    }

    void Update()
    {
        if (isDead) return;

        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        bool sprintPressed = sprintAction != null && sprintAction.IsPressed();

        // Si está atacando o locked -> no correr
        isRunning = (!isAttacking && !isMovementLocked) ? sprintPressed : false;

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;

        // Ataque (si quieres permitir atacar mientras locked, quita !isMovementLocked)
        if (!isAttacking && !isMovementLocked && attackAction != null && attackAction.WasPressedThisFrame())
            TryStartAttack();

        // Disparo: aunque esté locked por ataque, normalmente NO lo permitimos
        if (!isMovementLocked && shootAction != null && shootAction.WasPressedThisFrame())
            TryStartShoot();

        isGrounded = col.IsTouchingLayers(groundLayer);

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        if (animator != null)
        {
            float maxSpeed = isRunning ? runSpeed : walkSpeed;

            // Si está locked, la velocidad real será 0
            float effectiveVX = isMovementLocked ? 0f : rb.linearVelocity.x;
            float speed01 = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(effectiveVX));

            animator.SetFloat(speedParam, speed01);
            animator.SetBool(groundedParam, isGrounded);
            animator.SetBool(runParam, isRunning);
            if (!string.IsNullOrEmpty(deadBool)) animator.SetBool(deadBool, isDead);
        }

        // Flip: si está locked, mantén la orientación (no flippear por input)
        if (flipWithDirection && !isMovementLocked && Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(moveInput.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // ---- Lock timer ----
        if (isMovementLocked)
        {
            movementLockTimer -= Time.fixedDeltaTime;

            // Inmóvil en X (pero deja Y para gravedad/salto si quieres)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (movementLockTimer <= 0f)
                UnlockMovement();

            // Si está locked, no aplicar movimiento
            TryConsumeJump(); // si NO quieres saltar mientras locked, comenta esta línea
            return;
        }

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        // Ataque: también bloquea movimiento si lo deseas
        if (!(freezeMovementWhileAttacking && isAttacking))
        {
            rb.linearVelocity = new Vector2(moveInput.x * targetSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        TryConsumeJump();
    }

    void TryConsumeJump()
    {
        // Si NO quieres saltar mientras ataca/dispara, añade:
        // if (isAttacking || isMovementLocked) return;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            if (resetVerticalVelocityBeforeJump)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    // ---------- MOVEMENT LOCK ----------
    void LockMovement(float seconds, bool freezeXWithConstraints)
    {
        isMovementLocked = true;
        movementLockTimer = seconds;
        lockFreezesX = freezeXWithConstraints;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (freezeXWithConstraintsWhileLocked && freezeXWithConstraints)
        {
            // Mantén FreezeRotation si lo tenías
            baseConstraints = rb.constraints;
            rb.constraints = baseConstraints | RigidbodyConstraints2D.FreezePositionX;
        }
    }

    void UnlockMovement()
    {
        isMovementLocked = false;
        movementLockTimer = 0f;

        if (freezeXWithConstraintsWhileLocked && lockFreezesX)
        {
            rb.constraints = baseConstraints; // vuelve a lo que tenías
            lockFreezesX = false;
        }
    }

    // ---------- ATAQUE ----------
    void TryStartAttack()
    {
        if (isAttacking || isDead) return;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackCoroutine());
    }

    IEnumerator AttackCoroutine()
    {
        isAttacking = true;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        // ✅ Bloqueo total durante ataque
        if (freezeMovementWhileAttacking)
            LockMovement(attackDuration, freezeXWithConstraints: true);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackRoutine = null;

        // Si por algún motivo el lock sigue activo, lo soltamos
        if (isMovementLocked) UnlockMovement();
    }

    // ---------- SHOOT ----------
    void TryStartShoot()
    {
        if (isDead) return;
        if (!canShoot) return;

        if (animator != null && !string.IsNullOrEmpty(shootTrigger))
            animator.SetTrigger(shootTrigger);

        // ✅ Bloqueo mientras dispara (ajusta shootLockDuration a tu animación)
        LockMovement(shootLockDuration, freezeXWithConstraints: true);

        StartCoroutine(ShootCooldownRoutine());
    }

    IEnumerator ShootCooldownRoutine()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

    // Animation Event recomendado
    public void Anim_FireBullet() => SpawnBullet();
    public void FireBullet() => SpawnBullet();
    public void FireProjectile() => SpawnBullet();

    void SpawnBullet()
    {
        if (isDead) return;

        if (bulletPrefab == null || shootPoint == null)
        {
            if (debugLogs) Debug.LogWarning("[SHOOT] Falta bulletPrefab o shootPoint");
            return;
        }

        GameObject b = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);

        var rbB = b.GetComponent<Rigidbody2D>();
        if (rbB != null)
        {
            float facing = Mathf.Sign(transform.localScale.x);
            rbB.linearVelocity = new Vector2(facing * bulletSpeed, 0f);
        }

        // CLAVE: setear shooterTag y daño
        var p = b.GetComponent<Projectile>();
        if (p != null)
        {
            p.shooterTag = "Player";
            p.damage = bulletDamage;
        }
    }

    // Esto lo usa tu PlayerHealth
    public void OnHurt()
    {
        if (isDead) return;
        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);
    }

    public void OnDie()
    {
        if (isDead) return;

        isDead = true;

        UnlockMovement();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 🔥 CLAVE: bajar SOLO el sprite
        if (visual != null)
            visual.localPosition = visualStartLocalPos + new Vector3(0f, deathVisualYOffset, 0f);

        playerInput.currentActionMap?.Disable();

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deadBool))
                animator.SetBool(deadBool, true);

            if (!string.IsNullOrEmpty(dieTrigger))
                animator.SetTrigger(dieTrigger);
        }
    }

    public bool IsDead() => isDead;

    void OnGUI()
    {
        if (!debugHUD) return;
        GUI.Label(new Rect(10, 10, 500, 20), $"Grounded: {isGrounded}");
        GUI.Label(new Rect(10, 30, 700, 20),
            $"Run: {isRunning}  Attacking: {isAttacking}  Locked: {isMovementLocked}  Dead: {isDead}");
    }
}
