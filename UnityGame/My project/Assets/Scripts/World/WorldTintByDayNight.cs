using UnityEngine;

public class WorldTintByDayNight : MonoBehaviour
{
    [Header("References")]
    public DayNightManager dayNight;     // arrastra tu DayNightManager aquí
    public Transform worldRoot;          // arrastra tu World aquí

    [Header("Tint")]
    public Color dayTint = Color.white;
    public Color nightTint = new Color(0.65f, 0.75f, 1f, 1f); // azul suave
    [Range(0f, 1f)] public float affectStrength = 1f;

    SpriteRenderer[] renderers;

    void Awake()
    {
        if (worldRoot == null) worldRoot = transform;
        renderers = worldRoot.GetComponentsInChildren<SpriteRenderer>(true);
    }

    void LateUpdate()
    {
        if (dayNight == null) return;

        // 0 = día, 1 = noche
        float k = dayNight.IsNightNormalized;

        // mezcla
        Color target = Color.Lerp(dayTint, nightTint, k);

        // aplica (manteniendo alpha actual de cada renderer)
        foreach (var sr in renderers)
        {
            if (!sr) continue;
            var c = sr.color;
            var newC = Color.Lerp(Color.white, target, affectStrength);
            newC.a = c.a;
            sr.color = newC;
        }
    }
}
