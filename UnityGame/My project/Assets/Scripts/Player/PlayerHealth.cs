using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public float deathDestroyDelay = 1.2f; // pon aquí lo que dure tu anim de muerte

    int currentHealth;
    bool isDead;

    PlayerMovement2D pm;

    void Awake()
    {
        pm = GetComponent<PlayerMovement2D>();
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log("Player HP: " + currentHealth);

        // ? Hurt anim (si aún vive)
        if (currentHealth > 0)
        {
            if (pm != null) pm.OnHurt();
            return;
        }

        // ? Muere
        Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("PLAYER MUERTO");

        // ? dispara animación y bloquea input (lo hace tu PlayerMovement2D)
        if (pm != null) pm.OnDie();

        // ? espera a que termine la anim antes de destruir (o desactivar)
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDestroyDelay);

        // Opciones:
        //Destroy(gameObject);
        // o si prefieres no destruir:
        gameObject.SetActive(false);
    }
}
