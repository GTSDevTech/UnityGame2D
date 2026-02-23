using UnityEngine;

public class EnemyHealth_Boss : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 20;

    int currentHealth;
    bool isDead;

    // AI del boss (nuevo)
    BossAI_Demon ai;
    Rigidbody2D rb;

    void Awake()
    {
        // BossAI_Demon debe estar en el ROOT
        ai = GetComponent<BossAI_Demon>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(int damage)
    {
        // 🧱 Candado total: evita daño si ya murió
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"{name} (BOSS) HP: {currentHealth}");

        if (currentHealth > 0)
        {
            // 🔴 Solo hurt si sigue vivo
            if (ai != null)
                ai.PlayHurt();

            return;
        }

        Die();
    }

    void Die()
    {
        // 🧱 Evita doble muerte
        if (isDead) return;
        isDead = true;

        // 💀 1) CONGELAR FÍSICA PRIMERO (evita micro-caída)
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 💀 2) DESACTIVAR TODOS LOS COLLIDERS DEL BOSS
        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols)
            c.enabled = false;

        // 💀 3) Avisar a la IA para animación de muerte + evento cutscene
        if (ai != null)
        {
            ai.DieFromHealth();
        }
        else
        {
            // fallback si no hay AI
            Destroy(gameObject);
        }
    }
}