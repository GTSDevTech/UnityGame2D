using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class DayNightManager : MonoBehaviour
{
    [Header("Roots (optional manual assign)")]
    public GameObject skyDay;
    public GameObject skyNight;

    [Header("Auto-bind (names in Hierarchy)")]
    [SerializeField] private string skyDayName = "Sky Day";
    [SerializeField] private string skyNightName = "Sky Night";

    [Header("Timing")]
    public float fadeSeconds = 1.0f;
    public float daySeconds = 20f;
    public float nightSeconds = 20f;
    public bool startAtNight = false;

    // 0 day -> 1 night
    public float IsNightNormalized { get; private set; }

    [Header("Debug (optional)")]
    public bool allowManualToggleWithN = false;

    bool isNight;
    Coroutine cycleRoutine;

    void Awake()
    {
        AutoBind();
    }

    void OnEnable()
    {
        // Por si cambiaste escena y este objeto persiste o se reactivó
        AutoBind();
    }

    void Start()
    {
        if (skyDay == null || skyNight == null)
        {
            Debug.LogError("DayNightManager: No encuentro skyDay/skyNight. Asigna en Inspector o renombra en la jerarquía según skyDayName/skyNightName.", this);
            enabled = false;
            return;
        }

        // Ambos activos para poder hacer fade
        skyDay.SetActive(true);
        skyNight.SetActive(true);

        isNight = startAtNight;

        SetGroupAlpha(skyDay, isNight ? 0f : 1f);
        SetGroupAlpha(skyNight, isNight ? 1f : 0f);

        // Estado coherente siempre
        IsNightNormalized = GetAnyAlpha(skyNight);

        cycleRoutine = StartCoroutine(AutoCycle());
    }

    void Update()
    {
        if (!allowManualToggleWithN) return;

        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            if (cycleRoutine != null) StopCoroutine(cycleRoutine);
            StopAllCoroutines();
            StartCoroutine(SwitchTo(!isNight, restartCycle: true));
        }
    }

    void AutoBind()
    {
        if (skyDay == null)
            skyDay = FindRootByName(skyDayName);

        if (skyNight == null)
            skyNight = FindRootByName(skyNightName);
    }

    GameObject FindRootByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Busca exacto por nombre en escena
        var go = GameObject.Find(name);
        if (go) return go;

        // Fallback: busca por contains (por si tienes "Sky Day (1)")
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name == name || t.name.Contains(name))
                return t.gameObject;
        }
        return null;
    }

    IEnumerator AutoCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(isNight ? nightSeconds : daySeconds);
            yield return SwitchTo(!isNight, restartCycle: false);
        }
    }

    IEnumerator SwitchTo(bool toNight, bool restartCycle)
    {
        yield return FadeTo(toNight);

        isNight = toNight;

        SetGroupAlpha(skyDay, isNight ? 0f : 1f);
        SetGroupAlpha(skyNight, isNight ? 1f : 0f);

        IsNightNormalized = GetAnyAlpha(skyNight);

        if (restartCycle)
            cycleRoutine = StartCoroutine(AutoCycle());
    }

    IEnumerator FadeTo(bool toNight)
    {
        float t = 0f;

        float dayFrom = GetAnyAlpha(skyDay);
        float nightFrom = GetAnyAlpha(skyNight);

        float dayTo = toNight ? 0f : 1f;
        float nightTo = toNight ? 1f : 0f;

        float duration = Mathf.Max(0.01f, fadeSeconds);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);

            SetGroupAlpha(skyDay, Mathf.Lerp(dayFrom, dayTo, k));
            SetGroupAlpha(skyNight, Mathf.Lerp(nightFrom, nightTo, k));

            // Mantén este valor actualizado durante el fade
            IsNightNormalized = GetAnyAlpha(skyNight);

            yield return null;
        }

        SetGroupAlpha(skyDay, dayTo);
        SetGroupAlpha(skyNight, nightTo);

        IsNightNormalized = nightTo;
    }

    void SetGroupAlpha(GameObject root, float a)
    {
        if (!root) return;

        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        // Si es la noche, controla el alpha global de los twinkles
        if (root == skyNight)
        {
            var twinkles = root.GetComponentsInChildren<StarTwinkle>(true);
            foreach (var tw in twinkles)
                tw.globalAlpha = Mathf.InverseLerp(0.35f, 1f, a);
        }
    }

    float GetAnyAlpha(GameObject root)
    {
        if (!root) return 1f;
        var sr = root.GetComponentInChildren<SpriteRenderer>(true);
        return sr ? sr.color.a : 1f;
    }
}
