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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();

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
        isRunning = (!isAttacking) ? sprintPressed : false;

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;

        if (!isAttacking && attackAction != null && attackAction.WasPressedThisFrame())
            TryStartAttack();

        if (shootAction != null && shootAction.WasPressedThisFrame())
            TryStartShoot();

        isGrounded = col.IsTouchingLayers(groundLayer);

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        if (animator != null)
        {
            float maxSpeed = isRunning ? runSpeed : walkSpeed;
            float speed01 = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(rb.linearVelocity.x));

            animator.SetFloat(speedParam, speed01);
            animator.SetBool(groundedParam, isGrounded);
            animator.SetBool(runParam, isRunning);
            if (!string.IsNullOrEmpty(deadBool)) animator.SetBool(deadBool, isDead);
        }

        if (flipWithDirection && Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(moveInput.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        if (!(freezeMovementWhileAttacking && isAttacking))
        {
            rb.linearVelocity = new Vector2(moveInput.x * targetSpeed, rb.linearVelocity.y);
        }

        TryConsumeJump();
    }

    void TryConsumeJump()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            if (resetVerticalVelocityBeforeJump)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

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

        if (freezeMovementWhileAttacking)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackRoutine = null;
    }

    void TryStartShoot()
    {
        if (isDead) return;
        if (!canShoot) return;

        if (animator != null && !string.IsNullOrEmpty(shootTrigger))
            animator.SetTrigger(shootTrigger);

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
        GUI.Label(new Rect(10, 30, 500, 20), $"Run: {isRunning}  Attacking: {isAttacking}  Dead: {isDead}");
    }
}
