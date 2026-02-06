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

    [Header("Orientación")]
    public bool flipWithDirection = true;

    [Header("Animación")]
    [Tooltip("Velocidad mínima en X para considerar que está corriendo.")]
    public float runThreshold = 0.05f;

    [Header("Ground (opcional)")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.35f, 0.08f);

    [Header("Estado (solo lectura)")]
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private Vector2 currentVelocity;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        currentVelocity = rb.linearVelocity;

        // ✅ Animación basada en velocidad REAL (evita que "se quede corriendo")
        if (animator != null)
            animator.SetBool("Run", Mathf.Abs(currentVelocity.x) > runThreshold);

        // Flip basado en velocidad (más estable que moveInput)
        if (flipWithDirection && Mathf.Abs(currentVelocity.x) > runThreshold)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(currentVelocity.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if (resetVerticalVelocityBeforeJump)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
