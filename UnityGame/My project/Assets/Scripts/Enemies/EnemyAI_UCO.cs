using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI_Shooter : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Animator animator;

    [Header("Checks")]
    public Transform wallCheck;              // arrastra tu hijo WallCheck aquí
    public float wallCheckDistance = 0.25f;  // distancia del raycast
    public LayerMask wallLayer;              // capa Ground/Wall/etc.

    [Header("Animator Params (exactos)")]
    public string speedParam = "Speed";
    public string shootBool = "IsShooting";

    [Header("Velocidades")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float backOffSpeed = 2.0f;

    [Header("Detección")]
    public float visionRange = 7f;

    [Header("Distancias de combate")]
    public float stopDistance = 3.0f;
    public float tooCloseDistance = 2.2f;
    public float shootDistance = 3.5f;

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

        // Si no asignas wallCheck, intentamos encontrar uno con ese nombre
        if (wallCheck == null)
        {
            var wc = transform.Find("WallCheck");
            if (wc != null) wallCheck = wc;
        }
    }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

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

        // Si hay pared delante (en cualquier modo excepto disparando), girar
        if (shootTimer <= 0f && IsHittingWall())
        {
            TurnAround();
        }

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

        // Muy cerca -> alejarse
        if (dist <= tooCloseDistance)
        {
            StopShooting();

            // Si al alejarse hay pared, gira primero
            int awayDir = (player.position.x >= rb.position.x) ? -1 : 1;
            if (IsHittingWall(awayDir)) awayDir *= -1;

            rb.linearVelocity = new Vector2(awayDir * backOffSpeed, rb.linearVelocity.y);
            UpdateAnimator();
            return;
        }

        // Zona de parada -> quedarse y disparar
        if (dist <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;

            if (dist <= shootDistance && cooldownTimer <= 0f)
                StartShooting();

            UpdateAnimator();
            return;
        }

        // Lejos -> acercarse corriendo
        StopShooting();
        rb.linearVelocity = new Vector2(dir * runSpeed, rb.linearVelocity.y);
        UpdateAnimator();
    }

    // --------- Wall detection ----------
    bool IsHittingWall()
    {
        return IsHittingWall(dir);
    }

    bool IsHittingWall(int checkDir)
    {
        if (wallCheck == null) return false;

        Vector2 origin = wallCheck.position;
        Vector2 direction = Vector2.right * checkDir;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    void TurnAround()
    {
        dir *= -1;
        patrolStart = rb.position; // para que no se quede rebotando en el mismo sitio
        ApplyFlip();
    }

    // --------- Shooting ----------
    void StartShooting()
    {
        shootTimer = shootDuration;
        cooldownTimer = shootCooldown;
    }

    void StopShooting()
    {
        shootTimer = 0f;
    }

    // --------- Patrol ----------
    void Patrol()
    {
        rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);

        float dx = rb.position.x - patrolStart.x;
        if (Mathf.Abs(dx) >= patrolDistance)
        {
            TurnAround();
        }
    }

    // --------- Visual / Animator ----------
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

        // Raycast wall check
        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 to = wallCheck.position + Vector3.right * dir * wallCheckDistance;
            Gizmos.DrawLine(wallCheck.position, to);
        }
    }
}
