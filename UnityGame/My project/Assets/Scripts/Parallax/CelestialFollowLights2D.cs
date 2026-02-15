using UnityEngine;

public class CelestialFollowLights2D : MonoBehaviour
{
    public Transform target;      // player o camera (RECOMIENDO camera)
    public Transform sunVisual;
    public Transform moonVisual;

    public Transform sunLightFollow;   // objeto con Point Light 2D REAL
    public Transform moonLightFollow;  // objeto con Point Light 2D REAL

    [Header("Follow offset")]
    public float baseHeight = 7f;   // altura base de la luz
    public float xSwing = 4f;       // cuanto se desplaza en X según el ángulo
    public float smooth = 12f;

    void LateUpdate()
    {
        if (!target) return;

        if (sunVisual && sunLightFollow)
            MoveFollowLight(sunLightFollow, sunVisual);

        if (moonVisual && moonLightFollow)
            MoveFollowLight(moonLightFollow, moonVisual);
    }

    void MoveFollowLight(Transform followLight, Transform visual)
    {
        // Dirección “de dónde viene” (vector desde target hacia el sol visual)
        Vector3 dir = (visual.position - target.position);
        dir.z = 0f;

        // Normaliza y usamos solo la componente X para “ángulo lateral”
        float sx = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            sx = Mathf.Clamp(dir.normalized.x, -1f, 1f);

        Vector3 desired = target.position + new Vector3(sx * xSwing, baseHeight, 0f);
        desired.z = followLight.position.z;

        followLight.position = Vector3.Lerp(
            followLight.position,
            desired,
            1f - Mathf.Exp(-smooth * Time.deltaTime)
        );
    }
}
