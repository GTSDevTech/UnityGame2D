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

    [Header("Visual / Orientation")]
    [Tooltip("Arrastra aquí el hijo 'Visual'. Si está vacío, rotará el root.")]
    public Transform visual;

    [Tooltip("Si está activo, el sprite rota para mirar a la dirección de la velocidad.")]
    public bool orientToVelocity = true;

    [Tooltip("Offset en grados por si tu sprite no apunta exactamente a la derecha.")]
    public float rotationOffsetDegrees = 0f;

    [Tooltip("Velocidad mínima para actualizar la rotación (evita jitter cuando se para).")]
    public float minSpeedToRotate = 0.01f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (visual == null)
        {
            // Si existe hijo llamado "Visual", lo pillamos automático
            var v = transform.Find("Visual");
            if (v != null) visual = v;
        }
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void LateUpdate()
    {
        if (!orientToVelocity) return;
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < (minSpeedToRotate * minSpeedToRotate)) return;

        // Sprite apunta a la derecha => usamos "right" como dirección forward en 2D
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + rotationOffsetDegrees;

        Transform t = (visual != null) ? visual : transform;
        t.rotation = Quaternion.Euler(0f, 0f, angle);
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
