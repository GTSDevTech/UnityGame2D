using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;

    int currentHealth;
    bool isDead;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"{name} HP: {currentHealth}");

        if (currentHealth > 0) return;

        Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Aquí luego: animación, loot, score, etc.
        Destroy(gameObject);
    }
}
