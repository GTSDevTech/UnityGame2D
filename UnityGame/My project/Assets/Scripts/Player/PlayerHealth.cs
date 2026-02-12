using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public float deathAnimTime = 1.2f;

    int currentHealth;
    bool isDead;
    PlayerMovement2D pm;

    void Awake()
    {
        pm = GetComponent<PlayerMovement2D>();
    }

    void Start()
    {
        ResetHealth();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log("Player HP: " + currentHealth);

        if (currentHealth > 0)
        {
            if (pm != null) pm.OnHurt();
            return;
        }

        Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("PLAYER MUERTO");

        if (pm != null) pm.OnDie();

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathAnimTime);

        if (CheckpointManager.I != null)
            CheckpointManager.I.RespawnPlayer(transform);
        else
            Debug.LogWarning("No hay CheckpointManager, respawn cancelado.");
    }

    public void ResetAfterRespawn()
    {
        ResetHealth();
        isDead = false;

        if (pm != null)
            pm.OnRespawn();
    }

    void ResetHealth()
    {
        currentHealth = maxHealth;
    }
}
