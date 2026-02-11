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
    public string speedParam = "Speed";          // float (0-1)
    public string groundedParam = "isGrounded";  // bool
    public string runParam = "Run";              // bool
    public string attackTrigger = "Attack";      // trigger
    public string shootTrigger = "Shoot";        // trigger
    public string reloadTrigger = "Reload";      // trigger
    public string hurtTrigger = "Hurt";          // trigger
    public string dieTrigger = "Die";            // trigger
    public string deadBool = "IsDead";           // bool

    [Header("Ataque melee")]
    public float attackDuration = 0.35f;
    public bool freezeMovementWhileAttacking = true;

    [Header("Shoot")]
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 12f;
    public float shootCooldown = 0.25f;
    public int bulletDamage = 1;

    [Tooltip("Tiempo que el player queda inmóvil al disparar.")]
    public float shootLockDuration = 0.15f;

    [Header("Ammo / Reload (recarga)")]
    public bool enableReload = true;
    [Tooltip("Balas por cargador.")]
    public int magSize = 6;
    [Tooltip("Tiempo de recarga (si NO usas Animation Event de fin).")]
    public float reloadTime = 1.0f;
    [Tooltip("Bloquea movimiento durante recarga.")]
    public bool lockMovementWhileReloading = true;

    [Tooltip("Balas actuales en cargador.")]
    public int ammoInMag = 6;
    [Tooltip("Balas en reserva (lo que te dan los pickups).")]
    public int ammoReserve = 0;

    [Header("Lock de movimiento (general)")]
    [Tooltip("Si está activado, además de velocidad 0, congela X con constraints durante locks.")]
    public bool freezeXWithConstraintsWhileLocked = true;

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("Crouch (Agacharse)")]
    public bool enableCrouch = true;
    [Tooltip("Se considera agachado si moveInput.y <= crouchThreshold (mantener S / abajo).")]
    public float crouchThreshold = -0.5f;
    [Tooltip("Bool en el Animator para agacharse.")]
    public string crouchBoolParam = "IsCrouching";
    [Tooltip("Si está agachado, no se mueve.")]
    public bool freezeMovementWhileCrouching = true;
    [Tooltip("Permitir saltar estando agachado.")]
    public bool allowJumpWhileCrouching = false;
    [Tooltip("Permitir disparar estando agachado.")]
    public bool allowShootWhileCrouching = true;

    [Header("Debug")]
    public bool debugHUD = false;
    public bool debugLogs = false;

    [Header("Muerte (visual)")]
    public Transform visual;
    public float deathVisualYOffset = -0.25f;

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
    bool isCrouching;

    float coyoteCounter;
    float jumpBufferCounter;

    public int maletines;
    public int votos;

    Vector3 visualStartLocalPos;
    Coroutine attackRoutine;

    // ---- MOVEMENT LOCK ----
    bool isMovementLocked = false;
    float movementLockTimer = 0f;
    bool lockFreezesX = false;
    RigidbodyConstraints2D baseConstraints;

    // ---- RELOAD STATE ----
    bool isReloading = false;
    Coroutine reloadRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();

        baseConstraints = rb.constraints;

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

        // Asegura que el cargador no empiece por encima del magSize
        ammoInMag = Mathf.Clamp(ammoInMag, 0, magSize);
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

        // Ground
        isGrounded = col.IsTouchingLayers(groundLayer);

        // Crouch hold
        if (enableCrouch)
            isCrouching = isGrounded && (moveInput.y <= crouchThreshold);
        else
            isCrouching = false;

        if (animator != null && !string.IsNullOrEmpty(crouchBoolParam))
            animator.SetBool(crouchBoolParam, isCrouching);

        bool sprintPressed = sprintAction != null && sprintAction.IsPressed();

        // Si está atacando / locked / crouching / recargando -> no correr
        isRunning = (!isAttacking && !isMovementLocked && !isCrouching && !isReloading) ? sprintPressed : false;

        // Jump buffer
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;

        // Ataque (no durante recarga)
        if (!isAttacking && !isMovementLocked && !isReloading && attackAction != null && attackAction.WasPressedThisFrame())
            TryStartAttack();

        // Disparo (no durante recarga)
        if (!isMovementLocked && !isReloading && shootAction != null && shootAction.WasPressedThisFrame())
        {
            if (!isCrouching || allowShootWhileCrouching)
                TryStartShoot();
        }

        // Coyote / buffer
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        // Animator base
        if (animator != null)
        {
            float maxSpeed = isRunning ? runSpeed : walkSpeed;

            float effectiveVX =
                (isMovementLocked ||
                 (freezeMovementWhileCrouching && isCrouching) ||
                 (freezeMovementWhileAttacking && isAttacking) ||
                 (isReloading && lockMovementWhileReloading))
                ? 0f
                : rb.linearVelocity.x;

            float speed01 = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(effectiveVX));

            animator.SetFloat(speedParam, speed01);
            animator.SetBool(groundedParam, isGrounded);
            animator.SetBool(runParam, isRunning);
            if (!string.IsNullOrEmpty(deadBool)) animator.SetBool(deadBool, isDead);
        }

        // Flip
        if (flipWithDirection && !isMovementLocked && !isCrouching && !isReloading && Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(moveInput.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        isGrounded = col.IsTouchingLayers(groundLayer);

        // Lock timer (shoot/attack/reload)
        if (isMovementLocked)
        {
            movementLockTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (movementLockTimer <= 0f)
                UnlockMovement();

            TryConsumeJump();
            return;
        }

        // Recarga: inmóvil en X
        if (isReloading && lockMovementWhileReloading)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            //TryConsumeJump();
            return;
        }

        // Crouch: inmóvil en X
        if (freezeMovementWhileCrouching && isCrouching)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryConsumeJump();
            return;
        }

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        // Ataque: inmóvil en X si corresponde
        if (!(freezeMovementWhileAttacking && isAttacking))
            rb.linearVelocity = new Vector2(moveInput.x * targetSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        TryConsumeJump();
    }

    void TryConsumeJump()
    {
        if (isCrouching && !allowJumpWhileCrouching)
            return;

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
            rb.constraints = baseConstraints;
            lockFreezesX = false;
        }
    }

    // ---------- ATAQUE ----------
    void TryStartAttack()
    {
        if (isAttacking || isDead) return;
        if (isReloading) return;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackCoroutine());
    }

    IEnumerator AttackCoroutine()
    {
        isAttacking = true;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        if (freezeMovementWhileAttacking)
            LockMovement(attackDuration, freezeXWithConstraints: true);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackRoutine = null;

        if (isMovementLocked) UnlockMovement();
    }

    // ---------- SHOOT ----------
    void TryStartShoot()
    {
        if (isDead) return;
        if (isReloading) return;
        if (!canShoot) return;

        // Si no quedan balas en cargador, recarga si puedes
        if (enableReload && magSize > 0 && ammoInMag <= 0)
        {
            if (ammoReserve > 0) StartReload();
            return;
        }

        if (animator != null && !string.IsNullOrEmpty(shootTrigger))
            animator.SetTrigger(shootTrigger);

        LockMovement(shootLockDuration, freezeXWithConstraints: true);

        StartCoroutine(ShootCooldownRoutine());
        // El disparo real sale por Animation Event -> Anim_FireBullet()
    }

    IEnumerator ShootCooldownRoutine()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

    // ---------- RELOAD ----------
    void StartReload()
    {
        if (!enableReload) return;
        if (isReloading) return;

        // No recargues si ya está lleno
        if (ammoInMag >= magSize) return;

        // No recargues si no hay reserva
        if (ammoReserve <= 0) return;

        isReloading = true;
        canShoot = false;

        if (animator != null && !string.IsNullOrEmpty(reloadTrigger))
            animator.SetTrigger(reloadTrigger);

        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(reloadTime);
        FinishReload();
    }

    void FinishReload()
    {
        int need = magSize - ammoInMag;
        int take = Mathf.Min(need, ammoReserve);

        ammoInMag += take;
        ammoReserve -= take;

        isReloading = false;
        canShoot = true;

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }

        // por si acaso había un lock de disparo todavía activo:
        if (isMovementLocked) UnlockMovement();
    }

    // ✅ Si quieres, llama a esto desde un Animation Event al final del clip Reload
    public void Anim_ReloadComplete()
    {
        if (!isReloading) return;
        FinishReload();
    }

    // ---------- Disparo (Animation Event) ----------
    public void Anim_FireBullet() => SpawnBullet();
    public void FireBullet() => SpawnBullet();
    public void FireProjectile() => SpawnBullet();

    void SpawnBullet()
    {
        if (isDead) return;
        if (isReloading) return;

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

        var p = b.GetComponent<Projectile>();
        if (p != null)
        {
            p.shooterTag = "Player";
            p.damage = bulletDamage;
        }

        // ✅ Gasta 1 bala real
        if (enableReload && magSize > 0)
        {
            ammoInMag = Mathf.Max(0, ammoInMag - 1);

            if (ammoInMag <= 0 && ammoReserve > 0)
                StartReload();
        }
    }

    // ---------- POWERUPS ----------
    public void AgregarPowerUp(TipoPowerUp tipo)
    {
        switch (tipo)
        {
            case TipoPowerUp.Maletin:
                maletines++;
                break;

            case TipoPowerUp.Voto:
                votos++;
                break;

            case TipoPowerUp.Municion:
                RecargarMunicionExtra(6); // ✅ cada cargador suma 6 a reserva
                break;
        }
    }

    // Ahora munición extra = sumar a reserva
    public void RecargarMunicionExtra(int cantidad)
    {
        ammoReserve += cantidad;

        // ✅ Auto-recarga SOLO si estabas a 0
        if (enableReload && !isReloading && ammoReserve > 0 && ammoInMag <= 0)
            StartReload();

        if (debugLogs) Debug.Log($"[AMMO] +{cantidad} a reserva. Mag:{ammoInMag}/{magSize} Reserve:{ammoReserve}");
    }

    // ---------- Daño / muerte ----------
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

        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;

        isReloading = false;
        canShoot = false;

        UnlockMovement();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

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

        GUI.Label(new Rect(10, 10, 700, 20), $"Grounded: {isGrounded}");
        GUI.Label(new Rect(10, 30, 1300, 20),
            $"Move: {moveInput}  Crouch: {isCrouching}  Attacking: {isAttacking}  Reloading: {isReloading}  Ammo: {ammoInMag}/{magSize}  Reserve: {ammoReserve}  Locked: {isMovementLocked}  Dead: {isDead}");
    }
}
