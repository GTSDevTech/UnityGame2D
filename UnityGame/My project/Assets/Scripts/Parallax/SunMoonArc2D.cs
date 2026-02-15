using UnityEngine;

public class SunMoonArc2D : MonoBehaviour
{
    public DayNightManager dayNight;   // tu manager
    public Transform pivot;            // SkyPivot
    public Transform sunVisual;
    public Transform moonVisual;

    [Header("Arc")]
    public float radius = 12f;         // tamaño del arco
    public float heightOffset = 6f;    // sube/baja el arco respecto al pivot
    public float startAngleDeg = 0f;   // ajusta estética (0=sol a la derecha)

    void LateUpdate()
    {
        if (!dayNight || !pivot || !sunVisual || !moonVisual) return;

        // dayNight.IsNightNormalized: 0 día -> 1 noche
        float n = Mathf.Clamp01(dayNight.IsNightNormalized);

        // tDay: 0..1 (0 amanecer -> 1 atardecer aprox)
        float tDay = 1f - n;

        // Ángulo del sol en un semicírculo (izq->der o der->izq según prefieras)
        float aSun = Mathf.Lerp(180f, 0f, tDay) + startAngleDeg;  // sol recorre cielo
        float aMoon = aSun + 180f; // luna opuesta

        sunVisual.position  = ArcPoint(pivot.position, aSun);
        moonVisual.position = ArcPoint(pivot.position, aMoon);
    }

    Vector3 ArcPoint(Vector3 center, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Cos(rad) * radius;
        float y = Mathf.Sin(rad) * radius + heightOffset;
        return new Vector3(center.x + x, center.y + y, center.z);
    }
}
