using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 3f;
    public string shooterTag; // "Player" o "Enemy"

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignorar al que disparó
        if (!string.IsNullOrEmpty(shooterTag) && other.CompareTag(shooterTag))
            return;

        // Daño al Player
        if (other.CompareTag("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Daño al Enemy
        if (other.CompareTag("Enemy"))
        {
            var eh = other.GetComponent<EnemyAI_Shooter>();
            if (eh != null) eh.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Pared/Suelo
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
