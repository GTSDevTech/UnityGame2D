using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI_Shooter : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;                 // si está vacío, busca por Tag "Player"
    public Animator animator;

    [Header("Animator Params (exactos)")]
    public string speedParam = "Speed";      // Float
    public string shootBool = "IsShooting";  // Bool

    [Header("Velocidades")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float backOffSpeed = 2.0f;        // velocidad para alejarse si está muy cerca

    [Header("Detección")]
    public float visionRange = 7f;

    [Header("Distancias de combate")]
    public float stopDistance = 3.0f;        // distancia a la que quiere quedarse
    public float tooCloseDistance = 2.2f;    // si está más cerca que esto, se aleja
    public float shootDistance = 3.5f;       // si está dentro de esto, puede disparar

    [Header("Disparo (animación)")]
    public float shootDuration = 0.6f;
    public float shootCooldown = 0.9f;

    [Header("Patrulla")]
    public float patrolDistance = 2f;

    [Header("Flip")]
    public bool flipWithDirection = true;

    private Rigidbody2D rb;
    private Vector2 patrolStart;
    private int dir = 1;

    private float shootTimer = 0f;
    private float cooldownTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        patrolStart = rb.position;
    }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        // Seguridad: asegura relaciones lógicas
        if (tooCloseDistance > stopDistance) tooCloseDistance = stopDistance * 0.75f;
        if (shootDistance < stopDistance) shootDistance = stopDistance;
    }

    void FixedUpdate()
    {
        // Timers
        if (shootTimer > 0f) shootTimer -= Time.fixedDeltaTime;
        if (cooldownTimer > 0f) cooldownTimer -= Time.fixedDeltaTime;

        // Forzar animación de disparo según timer
        SetShootingBool(shootTimer > 0f);

        // Sin player -> patrulla
        if (player == null)
        {
            StopShooting();
            Patrol();
            UpdateAnimator();
            return;
        }

        float dist = Vector2.Distance(rb.position, player.position);
        bool sees = dist <= visionRange;

        // No lo ve -> patrulla
        if (!sees)
        {
            StopShooting();
            Patrol();
            UpdateAnimator();
            return;
        }

        // Mirar al player
        dir = player.position.x >= rb.position.x ? 1 : -1;
        ApplyFlip();

        // Si está disparando -> quieto
        if (shootTimer > 0f)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimator();
            return;
        }

        // Si está DEMASIADO cerca -> alejarse (no dispara a bocajarro)
        if (dist <= tooCloseDistance)
        {
            StopShooting();
            int awayDir = (player.position.x >= rb.position.x) ? -1 : 1; // dirección opuesta al player
            rb.linearVelocity = new Vector2(awayDir * backOffSpeed, rb.linearVelocity.y);
            UpdateAnimator();
            return;
        }

        // Si está dentro de la zona de parada (cerca pero no demasiado) -> quedarse y disparar
        if (dist <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;

            // Solo dispara si además está dentro de shootDistance (normalmente sí)
            if (dist <= shootDistance && cooldownTimer <= 0f)
                StartShooting();

            UpdateAnimator();
            return;
        }

        // Si está entre stopDistance y lejos -> acercarse (correr)
        StopShooting();
        rb.linearVelocity = new Vector2(dir * runSpeed, rb.linearVelocity.y);
        UpdateAnimator();
    }

    void StartShooting()
    {
        shootTimer = shootDuration;
        cooldownTimer = shootCooldown;
    }

    void StopShooting()
    {
        shootTimer = 0f;
    }

    void Patrol()
    {
        rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);

        float dx = rb.position.x - patrolStart.x;
        if (Mathf.Abs(dx) >= patrolDistance)
        {
            dir *= -1;
            patrolStart = rb.position;
            ApplyFlip();
        }
    }

    void ApplyFlip()
    {
        if (!flipWithDirection) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(speedParam, Mathf.Abs(rb.linearVelocity.x));
    }

    void SetShootingBool(bool value)
    {
        if (animator == null || string.IsNullOrEmpty(shootBool)) return;
        animator.SetBool(shootBool, value);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = new Color(1f, 0.4f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, shootDistance);
    }
}
