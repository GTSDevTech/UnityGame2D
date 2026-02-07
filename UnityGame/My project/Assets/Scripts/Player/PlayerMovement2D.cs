using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Movimiento Horizontal")]
    public float moveSpeed = 6f;

    [Header("Salto")]
    public float jumpForce = 12f;
    public bool resetVerticalVelocityBeforeJump = true;

    [Tooltip("Permite saltar un poquito después de dejar el suelo (segundos).")]
    public float coyoteTime = 0.1f;

    [Tooltip("Guarda el input de salto un poquito antes de tocar el suelo (segundos).")]
    public float jumpBufferTime = 0.1f;

    [Header("Orientación")]
    public bool flipWithDirection = true;

    [Header("Animación")]
    public float runThreshold = 0.05f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.35f, 0.08f);
    public LayerMask groundLayer;

    [Tooltip("Cuánto margen extra para detectar suelo (evita hundirse por 1 frame).")]
    public float groundSkin = 0.02f;

    [Header("Estado (solo lectura)")]
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private Vector2 currentVelocity;
    [SerializeField] private bool isGrounded;

    private Rigidbody2D rb;

    // Timers para salto estable
    private float coyoteCounter;
    private float jumpBufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        currentVelocity = rb.linearVelocity;

        // Grounded
        if (groundCheck != null)
        {
            var size = new Vector2(groundCheckSize.x, groundCheckSize.y + groundSkin);
            isGrounded = Physics2D.OverlapBox(groundCheck.position, size, 0f, groundLayer);
        }
        else
        {
            isGrounded = false;
        }

        // Coyote time
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        // Jump buffer
        jumpBufferCounter -= Time.deltaTime;

        // Animación Run
        if (animator != null)
            animator.SetBool("Run", Mathf.Abs(currentVelocity.x) > runThreshold);

        // Flip
        if (flipWithDirection && Mathf.Abs(currentVelocity.x) > runThreshold)
        {
            var s = transform.localScale;
            s.x = Mathf.Sign(currentVelocity.x) * Mathf.Abs(s.x);
            transform.localScale = s;
        }
    }

    void FixedUpdate()
    {
        // Movimiento
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // Ejecutar salto en física (estable)
        TryConsumeJump();
    }

    void TryConsumeJump()
    {
        // Si hay input guardado y aún estamos dentro del margen de coyote -> saltar
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            if (resetVerticalVelocityBeforeJump)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            // Consumir
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    // Input System: acción Move (Vector2)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Input System: acción Jump (Button)
    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        // Guardamos el salto (buffer) y FixedUpdate lo ejecuta si puede
        jumpBufferCounter = jumpBufferTime;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        var size = new Vector2(groundCheckSize.x, groundCheckSize.y + groundSkin);
        Gizmos.DrawWireCube(groundCheck.position, size);
    }
}
