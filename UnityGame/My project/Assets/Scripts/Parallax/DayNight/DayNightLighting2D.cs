using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class DayNightLighting2D : MonoBehaviour
{
    [Header("Refs (optional manual assign)")]
    public DayNightManager dayNight;
    public Light2D globalLight;
    public Light2D sunLight;   // Light2D del sol (puede ser SunLightFollow o el glow del SunVisual)
    public Light2D moonLight;  // Light2D de la luna (MoonLightFollow o glow del MoonVisual)

    [Header("Auto-bind (names in Hierarchy)")]
    [SerializeField] private string sunLightName = "SunLightFollow";
    [SerializeField] private string moonLightName = "MoonLightFollow";

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

    void Awake()
    {
        AutoBind();
    }

    void OnEnable()
    {
        // Por si entras en otra escena y hay nuevas instancias
        AutoBind();
    }

    void Reset()
    {
        AutoBind();
    }

    void AutoBind()
    {
        if (!dayNight)
            dayNight = FindFirstObjectByType<DayNightManager>();

        // Global light: intenta encontrar una Light2D de tipo Global
        if (!globalLight)
        {
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (l && l.lightType == Light2D.LightType.Global)
                {
                    globalLight = l;
                    break;
                }
            }
        }

        // Sun/Moon por nombre (lo normal en tu setup follow)
        if (!sunLight)
            sunLight = FindLight2DByName(sunLightName);

        if (!moonLight)
            moonLight = FindLight2DByName(moonLightName);

        // Fallback: si no existen esos nombres, intenta encontrar por nombres alternativos
        if (!sunLight)
            sunLight = FindLight2DByName("sun") ?? FindLight2DByName("Sun");

        if (!moonLight)
            moonLight = FindLight2DByName("moon") ?? FindLight2DByName("Moon");
    }

    Light2D FindLight2DByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var go = GameObject.Find(name);
        return go ? go.GetComponent<Light2D>() : null;
    }

    void Update()
    {
        if (!dayNight)
        {
            // Reintenta si cambiaste escena y el manager se creó después
            dayNight = FindFirstObjectByType<DayNightManager>();
            if (!dayNight) return;
        }

        if (!globalLight)
        {
            // Reintento suave
            AutoBind();
            if (!globalLight) return;
        }

        float n = Mathf.Clamp01(dayNight.IsNightNormalized); // 0 day -> 1 night
        float d = 1f - n;

        // Global
        globalLight.intensity = Mathf.Lerp(globalDayIntensity, globalNightIntensity, n);
        globalLight.color = Color.Lerp(globalDayColor, globalNightColor, n);

        // Sun / Moon
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
