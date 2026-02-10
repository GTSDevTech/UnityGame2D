using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 3f;

    void Start() => Destroy(gameObject, lifeTime);

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ejemplo: si el player tiene un script PlayerHealth con TakeDamage
        if (other.CompareTag("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
            Destroy(gameObject);
        }

        // Si choca con suelo/pared:
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            Destroy(gameObject);
    }
}
