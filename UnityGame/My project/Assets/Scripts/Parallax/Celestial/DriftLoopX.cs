using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DriftLoopX : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("World units per second")]
    public float speed = 0.2f;

    [Tooltip("If true, uses Camera bounds. If false, uses Manual Bounds below.")]
    public bool useCameraBounds = true;

    [Tooltip("If empty, uses Camera.main")]
    public Camera targetCamera;

    [Tooltip("Extra margin outside the bounds before wrapping")]
    public float padding = 0.5f;

    [Header("Manual Bounds (world X)")]
    public float manualMinX = -10f;
    public float manualMaxX = 10f;

    SpriteRenderer sr;
    float halfWidth;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
    }

    void Start()
    {
        // ancho real en mundo (incluye escala)
        halfWidth = sr.bounds.extents.x;
    }

    void Update()
    {
        // Move left (si quieres derecha, pon +speed)
        Vector3 pos = transform.position;
        pos.x += -speed * Time.deltaTime;

        GetBounds(out float minX, out float maxX);

        // Wrap: si sale por la izquierda, entra por la derecha
        if (pos.x < (minX - halfWidth - padding))
        {
            pos.x = (maxX + halfWidth + padding);
        }

        transform.position = pos;
    }

    void GetBounds(out float minX, out float maxX)
    {
        if (!useCameraBounds || targetCamera == null || !targetCamera.orthographic)
        {
            minX = manualMinX;
            maxX = manualMaxX;
            return;
        }

        float camHalfHeight = targetCamera.orthographicSize;
        float camHalfWidth = camHalfHeight * targetCamera.aspect;

        float camX = targetCamera.transform.position.x;
        minX = camX - camHalfWidth;
        maxX = camX + camHalfWidth;
    }
}
