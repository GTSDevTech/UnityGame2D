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

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("Debug")]
    public bool debugHUD = false;

    // Components
    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerInput playerInput;

    // Input Actions (leídos cada frame → no se quedan pillados)
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    // State
    private Vector2 moveInput;
    private bool isRunning;
    private bool isGrounded;

    // Jump helpers
    private float coyoteCounter;
    private float jumpBufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Referencias a actions (nombres EXACTOS del Input Action Asset)
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
    }

    void Update()
    {
        // --- INPUT (leído cada frame) ---
        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        isRunning = sprintAction != null && sprintAction.IsPressed();

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;

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

        // --- FLIP (por input, fiable) ---
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

        rb.linearVelocity = new Vector2(
            moveInput.x * targetSpeed,
            rb.linearVelocity.y
        );

        TryConsumeJump();
    }

    void TryConsumeJump()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            if (resetVerticalVelocityBeforeJump)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            if (animator != null)
                animator.SetTrigger("Jump"); // 👈 AQUÍ

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    void OnGUI()
    {
        if (!debugHUD) return;

        GUI.Label(new Rect(10, 10, 400, 20), $"Grounded: {isGrounded}");
        GUI.Label(new Rect(10, 30, 400, 20), $"Run: {isRunning}");
        GUI.Label(new Rect(10, 50, 400, 20), $"SpeedX: {Mathf.Abs(rb.linearVelocity.x):F2}");
        GUI.Label(new Rect(10, 70, 400, 20), $"Coyote: {coyoteCounter:F2}  Buffer: {jumpBufferCounter:F2}");
    }
}
