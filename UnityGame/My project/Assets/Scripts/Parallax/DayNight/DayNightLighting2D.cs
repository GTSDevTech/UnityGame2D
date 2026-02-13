using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightLighting2D : MonoBehaviour
{
    [Header("Refs")]
    public DayNightManager dayNight;
    public Light2D globalLight;
    public Light2D sunLight;   // Light2D en tu sprite "sun"
    public Light2D moonLight;  // Light2D en tu sprite "moon"

    [Header("Global (ambient)")]
    public float globalDayIntensity = 1.0f;
    public float globalNightIntensity = 0.35f;
    public Color globalDayColor = Color.white;
    public Color globalNightColor = new Color(0.55f, 0.65f, 1f, 1f);

    [Header("Sun")]
    public float sunMaxIntensity = 1.0f;
    public Color sunColor = new Color(1f, 0.95f, 0.75f, 1f);

    [Header("Moon")]
    public float moonMaxIntensity = 0.8f;
    public Color moonColor = new Color(0.65f, 0.75f, 1f, 1f);

    void Reset()
    {
        dayNight = FindFirstObjectByType<DayNightManager>();
    }

    void Update()
    {
        if (!dayNight || !globalLight) return;

        float n = Mathf.Clamp01(dayNight.IsNightNormalized); // 0 day -> 1 night
        float d = 1f - n;

        // Global
        globalLight.intensity = Mathf.Lerp(globalDayIntensity, globalNightIntensity, n);
        globalLight.color = Color.Lerp(globalDayColor, globalNightColor, n);

        // Sun / Moon (cruzadas)
        if (sunLight)
        {
            sunLight.intensity = sunMaxIntensity * d;
            sunLight.color = sunColor;
        }

        if (moonLight)
        {
            moonLight.intensity = moonMaxIntensity * n;
            moonLight.color = moonColor;
        }
    }
}
