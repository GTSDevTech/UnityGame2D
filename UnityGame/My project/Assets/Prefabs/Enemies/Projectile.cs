using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 3f;

    // Quién disparó ("Player" o "Enemy")
    public string shooterTag;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // No pegarle al que disparó
        if (!string.IsNullOrEmpty(shooterTag) && other.CompareTag(shooterTag))
            return;

        // PLAYER
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        // ENEMY
        if (other.CompareTag("Enemy"))
        {
            EnemyAI_Shooter enemy = other.GetComponent<EnemyAI_Shooter>();
            if (enemy != null)
                enemy.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        // SUELO / PAREDES
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
