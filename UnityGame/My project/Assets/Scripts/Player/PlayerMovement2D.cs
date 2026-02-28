using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    [Header("Beat'em up Depth (Z)")]
    [Tooltip("Componente que calcula la profundidad en Z y sorting. Si está null, no hay profundidad.")]
    public BeatEmUpDepth2D depth2D;

    [Header("Salto")]
    public float jumpForce = 12f;
    public bool resetVerticalVelocityBeforeJump = true;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Orientación")]
    public bool flipWithDirection = true;

    [Header("Tap-to-Flip / Hold-to-Move")]
    public float tapToFlipSeconds = 0.14f;
    public float moveDeadzone = 0.15f;

    [Header("Animator Params")]
    public string speedParam = "Speed";
    public string groundedParam = "isGrounded";
    public string runParam = "Run";
    public string attackTrigger = "Attack";
    public string shootTrigger = "Shoot";
    public string reloadTrigger = "Reload";
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";
    public string deadBool = "IsDead";

    [Header("Shoot Up / Aim (disparo arriba)")]
    public string aimUpBoolParam = "AimUp";
    public string shootUpTrigger = "ShootUp";
    public float aimUpThreshold = 0.5f;
    public float shootUpMaxSpeed = 0.1f;

    [Header("Special Move 1 (hold Crouch)")]
    public string specialMove1BoolParam = "SpecialMove1";
    public float specialHoldSeconds = 0.6f;

    [Header("Ataque melee")]
    public float attackDuration = 0.35f;
    public bool freezeMovementWhileAttacking = true;

    [Header("Melee Hitbox")]
    public Transform meleePoint;
    public Vector2 meleeSize = new Vector2(1.4f, 0.8f);
    public LayerMask enemyLayer;
    public int meleeDamage = 999; // 💀 Instakill
    bool meleeHitThisAttack = false;

    [Header("Shoot")]
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 12f;
    public float shootCooldown = 0.25f;
    public int bulletDamage = 1;

    public float shootLockDuration = 0.15f;

    [Header("Shoot - Spawn mode (robusto entre escenas)")]
    public bool fireUsingAnimationEvent = true;
    public float fireFallbackDelay = 0.08f;

    [Header("Ammo / Reload (recarga)")]
    public bool enableReload = true;
    public int magSize = 6;
    public float reloadTime = 1.0f;
    public bool lockMovementWhileReloading = true;

    public int ammoInMag = 6;
    public int ammoReserve = 0;

    [Header("Lock de movimiento (general)")]
    public bool freezeXWithConstraintsWhileLocked = true;

    [Header("Ground / Platforms")]
    public LayerMask groundLayer;

    [Header("Drop Through Platforms (opcional)")]
    public bool enableDropThroughPlatforms = true;
    public string platformPhysicsLayerName = "Platform";
    public float dropThroughSeconds = 0.25f;
    public float dropDownVelocity = 2.5f;
    public float dropDownPositionNudge = 0.03f;

    [Header("Crouch (Agacharse)")]
    public bool enableCrouch = true;

    [Tooltip("DEPRECATED: antes crouch era con Move.y <= threshold. Ahora se usa acción 'Crouch'. Se mantiene como fallback si no existe la acción.")]
    public float crouchThreshold = -0.5f;

    public string crouchBoolParam = "IsCrouching";
    public bool freezeMovementWhileCrouching = true;
    public bool allowJumpWhileCrouching = false;
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

    // Crouch como botón
    InputAction crouchAction;

    PlayerHealth playerHealth;

    Vector2 moveInput;
    float moveXForMotion = 0f;

    [Header("Sonidos")]
    public AudioSource shootSFX;
    public AudioSource jumpSFX;
    public AudioSource hitSFX;
    public AudioSource deathSFX;
    public AudioSource itemSFX;
    public AudioSource meleeSFX;
    public AudioSource fartSFX;

    // 🔊 AudioSource recarga (clip "recharge")
    public AudioSource rechargeSFX;

    bool isRunning;
    bool isGrounded;
    bool isAttacking;
    bool isDead;

    bool canShoot = true;
    bool shotPending = false;
    bool shotFiredThisPress = false;
    Coroutine shotFallbackRoutine;

    bool isCrouching;
    bool aimUp;

    Vector2 pendingShotDir = Vector2.zero;

    float downHoldTime = 0f;
    bool specialMove1Active = false;

    float coyoteCounter;
    float jumpBufferCounter;

    [Header("Progreso - Maletines / Votos")]
    public int maxMaletines = 20;
    public int maletines = 0;

    public int votos = 0;
    public int maxVotos = 6;

    Vector3 visualStartLocalPos;
    Coroutine attackRoutine;

    bool isMovementLocked = false;
    float movementLockTimer = 0f;
    bool lockFreezesX = false;
    RigidbodyConstraints2D baseConstraints;

    bool isReloading = false;
    Coroutine reloadRoutine;

    Coroutine dropRoutine;
    int cachedPlatformLayer = -1;

    bool holdingDir = false;
    float dirHoldTime = 0f;
    int heldDirSign = 0;
    bool movedThisHold = false;

    Coroutine reloadSfxFallbackRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();
        playerHealth = GetComponent<PlayerHealth>();

        baseConstraints = rb.constraints;

        if (!depth2D) depth2D = GetComponent<BeatEmUpDepth2D>();

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

        // Crouch botón (si existe)
        crouchAction = playerInput.actions.FindAction("Crouch", throwIfNotFound: false);

        ammoInMag = Mathf.Clamp(ammoInMag, 0, magSize);

        cachedPlatformLayer = LayerMask.NameToLayer(platformPhysicsLayerName);
        if (enableDropThroughPlatforms && cachedPlatformLayer < 0 && debugLogs)
            Debug.LogWarning($"[PLATFORM] No existe Physics Layer '{platformPhysicsLayerName}'.");
    }

    void OnEnable()
    {
        playerInput.currentActionMap?.Enable();
        moveAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        attackAction?.Enable();
        shootAction?.Enable();
        crouchAction?.Enable();
    }

    void OnDisable()
    {
        crouchAction?.Disable();
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

        // Grounded antes de lógica dependiente
        isGrounded = col.IsTouchingLayers(groundLayer);

        aimUp = moveInput.y >= aimUpThreshold;

        // Crouch por botón (si existe). Si no, fallback antiguo.
        bool crouchPressed = false;
        if (crouchAction != null)
            crouchPressed = crouchAction.IsPressed();
        else
            crouchPressed = (moveInput.y <= crouchThreshold);

        if (enableCrouch)
        {
            bool wasCrouching = isCrouching;
            isCrouching = isGrounded && crouchPressed;

            if (isCrouching && !wasCrouching && fartSFX != null)
                fartSFX.Play();
        }
        else
        {
            isCrouching = false;
        }

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(crouchBoolParam))
                animator.SetBool(crouchBoolParam, isCrouching);

            if (!string.IsNullOrEmpty(aimUpBoolParam))
                animator.SetBool(aimUpBoolParam, aimUp);
        }

        // SpecialMove1: hold crouch
        if (isCrouching && isGrounded && Mathf.Abs(rb.linearVelocity.x) <= shootUpMaxSpeed)
            downHoldTime += Time.deltaTime;
        else
            downHoldTime = 0f;

        specialMove1Active =
            isCrouching &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.x) <= shootUpMaxSpeed &&
            downHoldTime >= specialHoldSeconds;

        if (animator != null && !string.IsNullOrEmpty(specialMove1BoolParam))
            animator.SetBool(specialMove1BoolParam, specialMove1Active);

        UpdateTapFlipHoldMove();

        bool sprintPressed = sprintAction != null && sprintAction.IsPressed();
        isRunning = (!isAttacking && !isMovementLocked && !isCrouching && !isReloading) ? sprintPressed : false;

        bool jumpPressedThisFrame = (jumpAction != null && jumpAction.WasPressedThisFrame());
        if (jumpPressedThisFrame)
            jumpBufferCounter = jumpBufferTime;

        // ✅ DROP: Crouch + Jump (solo plataformas)
        if (enableDropThroughPlatforms && isGrounded && jumpPressedThisFrame)
        {
            if (crouchPressed)
            {
                if (TryDropThroughPlatform())
                    jumpBufferCounter = 0f;
            }
        }

        if (!isAttacking && !isMovementLocked && !isReloading && attackAction != null && attackAction.WasPressedThisFrame())
            TryStartAttack();

        if (!isMovementLocked && !isReloading && shootAction != null && shootAction.WasPressedThisFrame())
        {
            if (!isCrouching || allowShootWhileCrouching)
                TryStartShoot();
        }

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        if (animator != null)
        {
            animator.SetBool(groundedParam, isGrounded);
            animator.SetBool(runParam, isRunning);
            if (!string.IsNullOrEmpty(deadBool)) animator.SetBool(deadBool, isDead);
        }
    }

    void UpdateTapFlipHoldMove()
    {
        float rawX = moveInput.x;

        if (Mathf.Abs(rawX) < moveDeadzone)
        {
            holdingDir = false;
            dirHoldTime = 0f;
            heldDirSign = 0;
            movedThisHold = false;
            moveXForMotion = 0f;
            return;
        }

        int sign = rawX > 0f ? 1 : -1;

        if (!holdingDir || sign != heldDirSign)
        {
            holdingDir = true;
            heldDirSign = sign;
            dirHoldTime = 0f;
            movedThisHold = false;

            if (flipWithDirection && !isMovementLocked && !isReloading)
            {
                Vector3 scale = transform.localScale;
                scale.x = heldDirSign * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        dirHoldTime += Time.deltaTime;

        if (dirHoldTime < Mathf.Max(0.01f, tapToFlipSeconds))
        {
            moveXForMotion = 0f;
            return;
        }

        movedThisHold = true;
        moveXForMotion = rawX;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // refresco grounded en física
        isGrounded = col.IsTouchingLayers(groundLayer);

        if (isMovementLocked)
        {
            movementLockTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (movementLockTimer <= 0f)
                UnlockMovement();

            TryConsumeJump();
            UpdateAnimatorSpeedAfterPhysics();
            ApplyDepthZ(); // ✅ aplica Z aunque estés lockeado
            return;
        }

        if (isReloading && lockMovementWhileReloading)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimatorSpeedAfterPhysics();
            ApplyDepthZ();
            return;
        }

        // ✅ no bloquees si estamos en dropRoutine
        if (freezeMovementWhileCrouching && isCrouching && dropRoutine == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryConsumeJump();
            UpdateAnimatorSpeedAfterPhysics();
            ApplyDepthZ();
            return;
        }

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        if (!(freezeMovementWhileAttacking && isAttacking))
            rb.linearVelocity = new Vector2(moveXForMotion * targetSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        TryConsumeJump();
        UpdateAnimatorSpeedAfterPhysics();

        // ✅ Beat'em up depth en Z al final (robusto con Rigidbody2D)
        ApplyDepthZ();
    }

    void ApplyDepthZ()
    {
        if (depth2D == null || !depth2D.enabled) return;

        // Usamos Move.y como “profundidad” (Z), igual que querías
        depth2D.Tick(moveInput.y, Time.fixedDeltaTime);

        // Aplicar Z manteniendo X/Y de la física
        Vector2 p = rb.position;
        depth2D.ApplyZ(transform, p.x, p.y);
    }

    void UpdateAnimatorSpeedAfterPhysics()
    {
        if (animator == null || string.IsNullOrEmpty(speedParam)) return;

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
            if (jumpSFX != null) jumpSFX.Play();

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    // ===================== DROP THROUGH (FIX ROBUSTO) =====================
    bool TryDropThroughPlatform()
    {
        if (cachedPlatformLayer < 0) return false;

        float feetY = col.bounds.min.y;

        Vector2 center = new Vector2(col.bounds.center.x, feetY - 0.05f);
        Vector2 size = new Vector2(col.bounds.size.x * 0.8f, 0.12f);

        Collider2D platformCol = Physics2D.OverlapBox(
            center,
            size,
            0f,
            1 << cachedPlatformLayer
        );

        if (platformCol == null)
        {
            if (debugLogs) Debug.Log("[DROP] No se encontró collider de plataforma bajo los pies.");
            return false;
        }

        if (dropRoutine != null) StopCoroutine(dropRoutine);
        dropRoutine = StartCoroutine(DropThroughColliderRoutine(col, platformCol, dropThroughSeconds));

        rb.position += Vector2.down * Mathf.Max(0f, dropDownPositionNudge);

        float vy = rb.linearVelocity.y;
        if (vy > 0f) vy = 0f;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            -Mathf.Max(0.5f, dropDownVelocity)
        );

        if (debugLogs)
            Debug.Log($"[DROP] Ignorando colisión con {platformCol.name} durante {dropThroughSeconds:0.00}s.");

        return true;
    }

    IEnumerator DropThroughColliderRoutine(Collider2D playerCol, Collider2D platformCol, float seconds)
    {
        Physics2D.IgnoreCollision(playerCol, platformCol, true);

        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));

        if (playerCol && platformCol)
            Physics2D.IgnoreCollision(playerCol, platformCol, false);

        dropRoutine = null;
    }
    // ======================================================================

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
        meleeHitThisAttack = false;

        if (meleeSFX != null) meleeSFX.Play();

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        if (freezeMovementWhileAttacking)
            LockMovement(attackDuration, freezeXWithConstraints: true);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackRoutine = null;

        if (isMovementLocked) UnlockMovement();
    }

    public void Anim_MeleeHit()
    {
        if (isDead) return;
        if (meleeHitThisAttack) return;

        meleeHitThisAttack = true;

        Transform point = meleePoint != null ? meleePoint : transform;

        Collider2D[] hits = Physics2D.OverlapBoxAll(point.position, meleeSize, 0f, enemyLayer);

        foreach (var h in hits)
        {
            EnemyHealth eh = h.GetComponentInParent<EnemyHealth>();
            if (eh != null)
                eh.TakeDamage(meleeDamage);
        }
    }

    void TryStartShoot()
    {
        if (isDead) return;
        if (isReloading) return;
        if (!canShoot) return;

        if (enableReload && magSize > 0 && ammoInMag <= 0)
        {
            if (ammoReserve > 0) StartReload();
            return;
        }

        bool wantShootUp =
            aimUp &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.x) <= shootUpMaxSpeed &&
            !isCrouching;

        if (wantShootUp) pendingShotDir = Vector2.up;
        else
        {
            float facing = Mathf.Sign(transform.localScale.x);
            pendingShotDir = new Vector2(facing, 0f);
        }

        shotPending = true;
        shotFiredThisPress = false;

        if (shotFallbackRoutine != null) StopCoroutine(shotFallbackRoutine);
        shotFallbackRoutine = StartCoroutine(ShootFallbackRoutine());

        if (animator != null)
        {
            if (wantShootUp && !string.IsNullOrEmpty(shootUpTrigger))
                animator.SetTrigger(shootUpTrigger);
            else if (!string.IsNullOrEmpty(shootTrigger))
                animator.SetTrigger(shootTrigger);
        }

        LockMovement(shootLockDuration, freezeXWithConstraints: true);
        StartCoroutine(ShootCooldownRoutine());
    }

    IEnumerator ShootFallbackRoutine()
    {
        float delay = Mathf.Max(0f, fireFallbackDelay);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (!shotPending) yield break;

        if (!fireUsingAnimationEvent || !shotFiredThisPress)
        {
            shotPending = false;
            SpawnBullet();
        }
    }

    IEnumerator ShootCooldownRoutine()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

    void StartReload()
    {
        if (!enableReload) return;
        if (isReloading) return;

        animator.ResetTrigger(reloadTrigger);

        if (ammoInMag >= magSize) return;
        if (ammoReserve <= 0) return;

        isReloading = true;
        canShoot = false;

        if (animator != null && !string.IsNullOrEmpty(reloadTrigger))
            animator.SetTrigger(reloadTrigger);

        if (reloadSfxFallbackRoutine != null) StopCoroutine(reloadSfxFallbackRoutine);
        reloadSfxFallbackRoutine = StartCoroutine(ReloadSfxFallback(0.06f));

        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadSfxFallback(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        if (isReloading && rechargeSFX != null)
        {
            rechargeSFX.Stop();
            rechargeSFX.Play();
        }

        reloadSfxFallbackRoutine = null;
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

        if (isMovementLocked) UnlockMovement();
    }

    public void Anim_ReloadComplete()
    {
        if (!isReloading) return;
        FinishReload();
    }

    public void Anim_PlayReloadSFX()
    {
        Debug.Log("[EVENT] Anim_PlayReloadSFX (PlayerMovement2D) llamado");

        if (rechargeSFX != null)
        {
            rechargeSFX.Stop();
            rechargeSFX.Play();
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[RELOAD] rechargeSFX es NULL");
        }
    }

    public void Anim_FireBullet() => HandleAnimFireBullet();
    public void FireBullet() => HandleAnimFireBullet();
    public void FireProjectile() => HandleAnimFireBullet();

    void HandleAnimFireBullet()
    {
        shotFiredThisPress = true;
        shotPending = false;

        if (shotFallbackRoutine != null)
        {
            StopCoroutine(shotFallbackRoutine);
            shotFallbackRoutine = null;
        }

        SpawnBullet();
    }

    void SpawnBullet()
    {
        if (shootSFX != null) shootSFX.Play();
        if (isDead) return;
        if (isReloading) return;

        if (bulletPrefab == null || shootPoint == null)
        {
            if (debugLogs) Debug.LogWarning("[SHOOT] Falta bulletPrefab o shootPoint");
            return;
        }

        GameObject b = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);

        int projLayer = LayerMask.NameToLayer("Projectile_Player");
        if (projLayer >= 0)
        {
            b.layer = projLayer;
            foreach (Transform t in b.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = projLayer;
        }

        var rbB = b.GetComponent<Rigidbody2D>();
        if (rbB != null)
        {
            Vector2 dir = pendingShotDir.sqrMagnitude > 0.001f
                ? pendingShotDir.normalized
                : new Vector2(Mathf.Sign(transform.localScale.x), 0f);

            rbB.linearVelocity = dir * bulletSpeed;
        }

        var p = b.GetComponent<Projectile>();
        if (p != null)
        {
            p.shooterTag = "Player";
            p.damage = bulletDamage;
        }

        if (enableReload && magSize > 0)
        {
            ammoInMag = Mathf.Max(0, ammoInMag - 1);
        }
    }

    public void AddMaletin(int amount = 1)
    {
        if (itemSFX != null) itemSFX.Play();
        maxMaletines = Mathf.Max(1, maxMaletines);
        maletines = Mathf.Clamp(maletines + amount, 0, maxMaletines);
    }

    public void AgregarPowerUp(TipoPowerUp tipo)
    {
        switch (tipo)
        {
            case TipoPowerUp.Maletin:
                AddMaletin(1);
                break;

            case TipoPowerUp.Voto:
                votos = Mathf.Clamp(votos + 1, 0, maxVotos);
                if (playerHealth != null) playerHealth.Heal(1);
                break;

            case TipoPowerUp.Municion:
                RecargarMunicionExtra(6);
                break;
        }
    }

    public void RecargarMunicionExtra(int cantidad)
    {
        ammoReserve += cantidad;

        if (enableReload && !isReloading && ammoReserve > 0 && ammoInMag <= 0)
            StartReload();

        if (debugLogs) Debug.Log($"[AMMO] +{cantidad} a reserva. Mag:{ammoInMag}/{magSize} Reserve:{ammoReserve}");
    }

    public void OnHurt()
    {
        if (hitSFX != null) hitSFX.Play();
        if (isDead) return;
        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);
    }

    public void OnDie()
    {
        if (deathSFX != null) deathSFX.Play();
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

        StartCoroutine(LoadGameOverDelayed());
    }

    IEnumerator LoadGameOverDelayed()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("GameOverScene");
    }

    public bool IsDead() => isDead;

    public void OnRespawn()
    {
        isDead = false;

        playerInput.currentActionMap?.Enable();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deadBool))
                animator.SetBool(deadBool, false);

            animator.Rebind();
            animator.Update(0f);
        }

        if (visual != null)
            visual.localPosition = visualStartLocalPos;

        isMovementLocked = false;
        isReloading = false;
        canShoot = true;

        shotPending = false;
        shotFiredThisPress = false;
        if (shotFallbackRoutine != null)
        {
            StopCoroutine(shotFallbackRoutine);
            shotFallbackRoutine = null;
        }

        downHoldTime = 0f;
        specialMove1Active = false;
        if (animator != null && !string.IsNullOrEmpty(specialMove1BoolParam))
            animator.SetBool(specialMove1BoolParam, false);

        holdingDir = false;
        dirHoldTime = 0f;
        heldDirSign = 0;
        movedThisHold = false;
        moveXForMotion = 0f;

        // Reaplica Z (por si otros scripts lo dejaron a 0)
        ApplyDepthZ();
    }

    void OnGUI()
    {
        if (!debugHUD) return;

        GUI.Label(new Rect(10, 10, 900, 20), $"Grounded: {isGrounded}  Z:{transform.position.z:0.000}");
        GUI.Label(new Rect(10, 30, 1600, 20),
            $"Move:{moveInput}  MoveXForMotion:{moveXForMotion:0.00}  Crouch:{isCrouching}  Attacking:{isAttacking}  Reloading:{isReloading}  Ammo:{ammoInMag}/{magSize}  Reserve:{ammoReserve}  Locked:{isMovementLocked}  Dead:{isDead}  AimUp:{aimUp}  Special:{specialMove1Active}");
    }
}