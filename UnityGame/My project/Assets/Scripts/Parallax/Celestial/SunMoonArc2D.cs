using UnityEngine;

public class SunMoonArc2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orbitCenter;   // SunOrbitCenter
    [SerializeField] private Transform sunVisual;
    [SerializeField] private Transform moonVisual;

    [Header("Arc Settings")]
    [SerializeField] private float radius = 200f;     // tamaño del arco (ajustaremos luego)
    [SerializeField] private float cycleDuration = 60f; // segundos para ciclo completo
    [SerializeField] private float startAngleOffset = 0f;

    private float time;

    void Update()
    {
        if (!orbitCenter || !sunVisual || !moonVisual)
            return;

        time += Time.deltaTime;

        float normalized = (time % cycleDuration) / cycleDuration;
        float angle = normalized * 360f + startAngleOffset;

        UpdateBody(sunVisual, angle);
        UpdateBody(moonVisual, angle + 180f);
    }

    void UpdateBody(Transform body, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * radius,
            Mathf.Sin(rad) * radius,
            0f
        );

        body.position = orbitCenter.position + offset;
    }
}
