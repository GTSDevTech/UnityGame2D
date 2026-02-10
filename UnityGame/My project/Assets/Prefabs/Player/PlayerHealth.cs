using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Animator")]
    public Animator animator;
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";
    public string deadBool = "IsDead";

    [Header("Invulnerabilidad")]
    public bool useIFrames = true;
    public float iFrameTime = 0.4f;

    bool isDead = false;
    bool invulnerable = false;

    PlayerMovement2D movement;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        movement = GetComponent<PlayerMovement2D>();
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (useIFrames && invulnerable) return;

        currentHealth -= damage;
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // HURT
        if (movement != null)
            movement.OnHurt(); // usa tu método del movement si lo tienes
        else if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        if (useIFrames)
            StartCoroutine(IFrames());
    }

    IEnumerator IFrames()
    {
        invulnerable = true;
        yield return new WaitForSeconds(iFrameTime);
        invulnerable = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player muerto");

        // Muerte (bloquea input/movimiento si tienes PlayerMovement2D editado)
        if (movement != null)
        {
            movement.OnDie();
        }
        else
        {
            // Si no tienes OnDie, al menos dispara animación
            if (animator != null)
            {
                if (!string.IsNullOrEmpty(deadBool)) animator.SetBool(deadBool, true);
                if (!string.IsNullOrEmpty(dieTrigger)) animator.SetTrigger(dieTrigger);
            }
        }

        // Opcional: desactivar collider para que no reciba más hits
        // var col = GetComponent<Collider2D>();
        // if (col) col.enabled = false;
    }

    // Opcional: para curar / reiniciar
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        invulnerable = false;

        if (animator != null && !string.IsNullOrEmpty(deadBool))
            animator.SetBool(deadBool, false);
    }
}
