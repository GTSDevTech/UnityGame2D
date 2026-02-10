using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI_Shooter : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Tooltip("Animator (si lo tienes en un hijo Visual, asígnalo aquí o se auto-busca)")]
    public Animator animator;

    [Tooltip("Hijo que contiene Sprite/Animator (opcional). Si existe, se baja al morir para que no “flote”.")]
    public Transform visual;

    [Header("Checks")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.25f;
    public LayerMask wallLayer;

    [Header("Animator Params (exactos)")]
    public string speedParam = "Speed";
    public string shootBool = "IsShooting";
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";     // opcional (si lo usas)
    public string deadBool = "IsDead";    // recomendado

    [Header("Vida")]
    public int maxHealth = 3;
    public float hurtStunTime = 0.25f;

    [Header("Disparo (proyectil)")]
    public Transform shootPoint;
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public int projectileDamage = 1;

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

    [Header("Muerte")]
    public float deathDisableDelay = 1.5f;

    [Header("Fix visual muerte (si “flota”)")]
    public float deathVisualYOffset = -0f; // ajusta a ojo (ya te funcionó)
    Vector3 visualStartLocalPos;

    Rigidbody2D rb;
    Vector2 patrolStart;
    int dir = 1;

    float shootTimer = 0f;
    float cooldownTimer = 0f;
    float deathLockedY;

    int health;
    bool isDead = false;
    bool isStunned = false;
    Coroutine deathRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        patrolStart = rb.position;
        health = maxHealth;

        // Auto asignar Visual
        if (visual == null)
        {
            var v = transform.Find("Visual");
            if (v != null) visual = v;
        }
        if (visual != null)
            visualStartLocalPos = visual.localPosition;

        // Auto asignar Animator (si está en Visual o en el mismo GO)
        if (animator == null)
        {
            animator = (visual != null) ? visual.GetComponent<Animator>() : GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        if (wallCheck == null)
        {
            var wc = transform.Find("WallCheck");
            if (wc != null) wallCheck = wc;
        }

        if (shootPoint == null)
        {
            var sp = transform.Find("ShootPoint");
            if (sp != null) shootPoint = sp;
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
        if (isDead) return;

        // stun breve al recibir daño
        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimator();
            return;
        }

        // Timers
        if (shootTimer > 0f) shootTimer -= Time.fixedDeltaTime;
        if (cooldownTimer > 0f) cooldownTimer -= Time.fixedDeltaTime;

        // Forzar animación de disparo según timer
        SetShootingBool(shootTimer > 0f);

        // Si hay pared delante (en cualquier modo excepto disparando), girar
        if (shootTimer <= 0f && IsHittingWall())
            TurnAround();

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

    // ---------- VIDA / DAÑO ----------
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        health -= amount;

        if (health <= 0)
        {
            Die();
            return;
        }

        StopShooting();
        SetShootingBool(false);
        rb.linearVelocity = Vector2.zero;

        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        StartCoroutine(HurtStun());
    }

    IEnumerator HurtStun()
    {
        isStunned = true;
        yield return new WaitForSeconds(hurtStunTime);
        isStunned = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        StopShooting();
        SetShootingBool(false);

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deadBool)) animator.SetBool(deadBool, true);
            // si usas trigger, ok:
            if (!string.IsNullOrEmpty(dieTrigger)) animator.SetTrigger(dieTrigger);
        }

        // parar física
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 🔒 guardar Y exacta al morir
        deathLockedY = rb.position.y;

        // 🔒 bloquear Y + rotación (evita hundirse y rebotar)
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        // forzar posición una vez (evita corrección tardía)
        rb.position = new Vector2(rb.position.x, deathLockedY);

        // collider sólido para que apoye en el suelo
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = false;

        // ajuste collider para "tumbado" (si lo estabas usando)
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(box.size.x * 1.4f, box.size.y * 0.35f);
            box.offset = new Vector2(box.offset.x, box.offset.y - 0.35f);
        }

        // ✅ Fix visual: baja SOLO el hijo Visual (evita “flotar” visualmente)
        if (visual != null)
            visual.localPosition = visualStartLocalPos + new Vector3(0f, deathVisualYOffset, 0f);

        if (deathRoutine != null) StopCoroutine(deathRoutine);
        deathRoutine = StartCoroutine(DisableAfterDeath());
    }

    IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(deathDisableDelay);
        this.enabled = false;
        // (si quieres) gameObject.SetActive(false);
        // (si quieres) Destroy(gameObject);
    }

    // ---------- DISPARO ----------
    void StartShooting()
    {
        shootTimer = shootDuration;
        cooldownTimer = shootCooldown;
        // si NO usas Animation Event: FireProjectile();
    }

    // Llama a este método desde un Animation Event en el clip de disparo
    public void FireProjectile()
    {
        if (isDead) return;
        if (projectilePrefab == null || shootPoint == null) return;

        GameObject b = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

        var rbB = b.GetComponent<Rigidbody2D>();
        if (rbB != null)
            rbB.linearVelocity = new Vector2(dir * projectileSpeed, 0f);

        var p = b.GetComponent<Projectile>();
        if (p != null)
        {
            p.damage = projectileDamage;
            p.shooterTag = "Enemy";
        }
    }

    void StopShooting() => shootTimer = 0f;

    // ---------- Wall detection ----------
    bool IsHittingWall() => IsHittingWall(dir);

    bool IsHittingWall(int checkDir)
    {
        if (wallCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * checkDir, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    void TurnAround()
    {
        dir *= -1;
        patrolStart = rb.position;
        ApplyFlip();
    }

    // ---------- Patrol ----------
    void Patrol()
    {
        rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);

        float dx = rb.position.x - patrolStart.x;
        if (Mathf.Abs(dx) >= patrolDistance)
            TurnAround();
    }

    // ---------- Visual / Animator ----------
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
}
