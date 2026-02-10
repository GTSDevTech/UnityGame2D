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
    public string speedParam = "Speed";          // float 0..1
    public string groundedParam = "isGrounded";  // bool
    public string runParam = "Run";              // bool
    public string attackTrigger = "Attack";      // trigger

    [Header("Ataque")]
    public float attackDuration = 0.35f;         // ajusta a la duración real del clip
    public bool freezeMovementWhileAttacking = true;

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("Debug")]
    public bool debugHUD = false;

    // Components
    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerInput playerInput;

    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction attackAction;

    // State
    private Vector2 moveInput;
    private bool isRunning;
    private bool isGrounded;
    private bool isAttacking;

    // Jump helpers
    private float coyoteCounter;
    private float jumpBufferCounter;

    private Coroutine attackRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Nombres EXACTOS del Input Action Asset
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
        attackAction = playerInput.actions["Attack"];

        Debug.Log($"[INPUT] ActionMap activo: {playerInput.currentActionMap?.name}");
        Debug.Log($"[INPUT] Attack action encontrada: {(attackAction != null ? attackAction.name : "NO")}");
    }

    void OnEnable()
    {
        // Habilitar actions (más correcto que hacerlo solo en Awake)
        playerInput.currentActionMap?.Enable();
        moveAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        attackAction?.Enable();
    }

    void OnDisable()
    {
        attackAction?.Disable();
        sprintAction?.Disable();
        jumpAction?.Disable();
        moveAction?.Disable();
        playerInput.currentActionMap?.Disable();
    }

    void Update()
    {
        // --- INPUT ---
        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        bool sprintPressed = sprintAction != null && sprintAction.IsPressed();
        isRunning = (!isAttacking) ? sprintPressed : false;

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;

        if (!isAttacking && attackAction != null && attackAction.WasPressedThisFrame())
        {
            Debug.Log("[INPUT] ATTACK PRESSED");
            TryStartAttack();
        }

        // --- GROUND ---
        isGrounded = col.IsTouchingLayers(groundLayer);

        // Coyote time
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        // Jump buffer timer
        jumpBufferCounter -= Time.deltaTime;

        // --- ANIMATOR ---
        if (animator != null)
        {
            float maxSpeed = isRunning ? runSpeed : walkSpeed;
            float speed01 = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(rb.linearVelocity.x));

            animator.SetFloat(speedParam, speed01);
            animator.SetBool(groundedParam, isGrounded);
            animator.SetBool(runParam, isRunning);
        }

        // --- FLIP ---
        if (flipWithDirection && Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(moveInput.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        if (!(freezeMovementWhileAttacking && isAttacking))
        {
            rb.linearVelocity = new Vector2(
                moveInput.x * targetSpeed,
                rb.linearVelocity.y
            );
        }

        TryConsumeJump();
    }

    void TryConsumeJump()
    {
        // Si quieres impedir saltar mientras atacas:
        // if (isAttacking) return;

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
        if (isAttacking) return;

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

    void OnGUI()
    {
        if (!debugHUD) return;

        GUI.Label(new Rect(10, 10, 500, 20), $"Grounded: {isGrounded}");
        GUI.Label(new Rect(10, 30, 500, 20), $"Run: {isRunning}  Attacking: {isAttacking}");
        GUI.Label(new Rect(10, 50, 500, 20), $"SpeedX: {Mathf.Abs(rb.linearVelocity.x):F2}  VelY: {rb.linearVelocity.y:F2}");
        GUI.Label(new Rect(10, 70, 500, 20), $"Coyote: {coyoteCounter:F2}  Buffer: {jumpBufferCounter:F2}");
    }
}
