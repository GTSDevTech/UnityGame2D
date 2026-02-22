using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;

    int currentHealth;
    bool isDead;

    EnemyAI_Shooter ai;
    Rigidbody2D rb;

    void Awake()
    {
        // EnemyAI_Shooter debe estar en el ROOT
        ai = GetComponent<EnemyAI_Shooter>();
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
        Debug.Log($"{name} HP: {currentHealth}");

        if (currentHealth > 0)
        {
            // 🔴 Solo animación de hurt si sigue vivo
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
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 💀 2) DESACTIVAR TODOS LOS COLLIDERS DEL ENEMIGO
        // (esto permite atravesarlo)
        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);

        foreach (var c in cols)
            c.enabled = false;

        // 💀 3) Avisar a la IA para animación de muerte
        if (ai != null)
        {
            ai.DieFromHealth();
        }
        else
        {
            // fallback si no hay IA
            Destroy(gameObject);
        }
    }
}