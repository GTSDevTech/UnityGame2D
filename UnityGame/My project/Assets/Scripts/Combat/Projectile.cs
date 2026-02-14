using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float lifeTime = 3f;

    [Tooltip("Tag del que dispara: \"Player\" o \"Enemy\". Evita autohit incluso si golpea un hijo.")]
    public string shooterTag;

    [Header("World Hit")]
    public bool destroyOnGround = true;
    public string[] worldLayers = new[] { "Ground", "Platform" };

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1) Ignorar al que disparó (recorriendo padres: root/hitbox/visual)
        if (!string.IsNullOrEmpty(shooterTag))
        {
            Transform t = other.transform;
            while (t != null)
            {
                if (t.CompareTag(shooterTag))
                    return;

                t = t.parent;
            }
        }

        // 2) Daño por Health en padres (funciona aunque el collider golpeado sea hijo)
        EnemyHealth eh = other.GetComponentInParent<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 3) Mundo (Ground/Platform)
        if (destroyOnGround)
        {
            int hitLayer = other.gameObject.layer;
            for (int i = 0; i < worldLayers.Length; i++)
            {
                int l = LayerMask.NameToLayer(worldLayers[i]);
                if (l >= 0 && hitLayer == l)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
