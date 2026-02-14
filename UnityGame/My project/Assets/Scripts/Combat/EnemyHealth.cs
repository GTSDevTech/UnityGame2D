using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;

    int currentHealth;
    bool isDead;

    EnemyAI_Shooter ai;

    void Awake()
    {
        ai = GetComponent<EnemyAI_Shooter>(); // debe estar en ROOT
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"{name} HP: {currentHealth}");

        if (currentHealth > 0)
        {
            if (ai != null) ai.PlayHurt();
            return;
        }

        Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (ai != null) ai.DieFromHealth();
        else Destroy(gameObject); // fallback
    }
}
