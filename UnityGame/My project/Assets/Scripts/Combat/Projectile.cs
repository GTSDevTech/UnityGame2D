using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float lifeTime = 3f;

    [Tooltip("Tag del que dispara: \"Player\" o \"Enemy\" (evita autohit por tag).")]
    public string shooterTag;

    [Header("World Hit")]
    public bool destroyOnGround = true;

    [Tooltip("Si tu suelo/plataformas usan layers distintas, ponlas aquí.")]
    public string[] worldLayers = new[] { "Ground", "Platform" };

    // (Preparado para futuro: partículas)
    // public ParticleSystem bloodVfxPrefab;
    // public ParticleSystem worldHitVfxPrefab;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Root lógico: si el collider está en un hijo, buscamos el rigidbody/parent
        Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;

        // Ignorar al que disparó (IMPORTANTE: comparar en el root, no en el hijo)
        if (!string.IsNullOrEmpty(shooterTag) && root.CompareTag(shooterTag))
            return;

        // ====== DAÑO PLAYER ======
        if (root.CompareTag("Player"))
        {
            var ph = root.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);

                // Futuro sangre:
                // SpawnBlood(other.ClosestPoint(transform.position));

                Destroy(gameObject);
                return;
            }
        }

        // ====== DAÑO ENEMY ======
        if (root.CompareTag("Enemy"))
        {
            // Nueva forma recomendada: EnemyHealth
            var eh = root.GetComponentInParent<EnemyHealth>();
            if (eh != null)
            {
                eh.TakeDamage(damage);

                // Futuro sangre:
                // SpawnBlood(other.ClosestPoint(transform.position));

                Destroy(gameObject);
                return;
            }

            // Compatibilidad si aún usas EnemyAI_Shooter con TakeDamage
            var ai = root.GetComponentInParent<EnemyAI_Shooter>();
            if (ai != null)
            {
                ai.TakeDamage(damage);

                Destroy(gameObject);
                return;
            }
        }

        // ====== MUNDO (SUELO/PARED/PLATAFORMA) ======
        if (destroyOnGround)
        {
            int hitLayer = other.gameObject.layer;
            for (int i = 0; i < worldLayers.Length; i++)
            {
                int l = LayerMask.NameToLayer(worldLayers[i]);
                if (l >= 0 && hitLayer == l)
                {
                    // Futuro partículas de impacto:
                    // SpawnWorldHit(other.ClosestPoint(transform.position));

                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    // void SpawnBlood(Vector2 hitPoint)
    // {
    //     if (bloodVfxPrefab != null)
    //         Instantiate(bloodVfxPrefab, hitPoint, Quaternion.identity);
    // }
}
