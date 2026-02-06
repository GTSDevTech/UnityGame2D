using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    // =========================
    // MOVIMIENTO HORIZONTAL
    // =========================
    [Header("Movimiento Horizontal")]
    [Tooltip("Velocidad de movimiento izquierda/derecha")]
    public float moveSpeed = 6f;

    // =========================
    // SALTO
    // =========================
    [Header("Salto")]
    [Tooltip("Fuerza del salto (impulso hacia arriba)")]
    public float jumpForce = 12f;

    [Tooltip("Reinicia la velocidad vertical antes de saltar")]
    public bool resetVerticalVelocityBeforeJump = true;

    // =========================
    // ROTACIÓN / ORIENTACIÓN
    // =========================
    [Header("Orientación")]
    [Tooltip("Voltear el personaje según dirección")]
    public bool flipWithDirection = true;

    // =========================
    // REFERENCIAS (DEBUG)
    // =========================
    [Header("Estado (solo lectura)")]
    [SerializeField] private Vector2 moveInput;   // visible pero no editable
    [SerializeField] private Vector2 currentVelocity;

    Rigidbody2D rb;

    // =========================
    // UNITY EVENTS
    // =========================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Guardamos velocidad actual solo para verla en Inspector
        currentVelocity = rb.linearVelocity;

        // Voltear sprite según dirección
        if (flipWithDirection && Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(moveInput.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y
        );
    }

    // =========================
    // INPUT SYSTEM
    // =========================

    // Acción: Move (Value / Vector2)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Acción: Jump (Button)
    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if (resetVerticalVelocityBeforeJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
}
