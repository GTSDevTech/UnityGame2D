using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DriftLoopX : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("World units per second (ej: 0.15 - 0.6 dependiendo del PPU).")]
    public float speed = 0.2f;

    [Tooltip("Si true, se mueve a la derecha. Si false, a la izquierda.")]
    public bool moveRight = false;

    [Header("Bounds")]
    [Tooltip("BoxCollider2D que define el área visible/permitida (por ejemplo: CameraBounds).")]
    public BoxCollider2D boundsCollider;

    [Tooltip("Margen extra para que no aparezca cortada al hacer wrap.")]
    public float padding = 0.5f;

    SpriteRenderer sr;
    float halfWidth;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        RecalcHalfWidth();
    }

    void OnEnable()
    {
        RecalcHalfWidth();
    }

    void RecalcHalfWidth()
    {
        // ancho real en mundo (incluye escala)
        halfWidth = sr.bounds.extents.x;
    }

    void Update()
    {
        if (boundsCollider == null) return;

        float dir = moveRight ? 1f : -1f;

        Vector3 pos = transform.position;
        pos.x += dir * speed * Time.deltaTime;

        // bounds del collider en world
        Bounds b = boundsCollider.bounds;
        float minX = b.min.x;
        float maxX = b.max.x;

        // Wrap
        if (dir > 0f)
        {
            // va a la derecha, si sale por la derecha -> entra por la izquierda
            if (pos.x > (maxX + halfWidth + padding))
                pos.x = (minX - halfWidth - padding);
        }
        else
        {
            // va a la izquierda, si sale por la izquierda -> entra por la derecha
            if (pos.x < (minX - halfWidth - padding))
                pos.x = (maxX + halfWidth + padding);
        }

        transform.position = pos;
    }
}