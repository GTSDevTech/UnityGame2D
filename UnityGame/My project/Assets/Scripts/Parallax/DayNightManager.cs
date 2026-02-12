using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DayNightManager : MonoBehaviour
{
    [Header("Roots")]
    public GameObject skyDay;
    public GameObject skyNight;

    [Header("Timing")]
    public float fadeSeconds = 1.0f;
    public float daySeconds = 20f;
    public float nightSeconds = 20f;
    public bool startAtNight = false;

    public float IsNightNormalized { get; private set; }

    [Header("Debug (optional)")]
    public bool allowManualToggleWithN = false;

    bool isNight;
    Coroutine cycleRoutine;

    void Start()
    {
        if (skyDay == null || skyNight == null)
        {
            Debug.LogError("DayNightManager: Asigna skyDay y skyNight en el Inspector.");
            enabled = false;
            return;
        }

        // Ambos activos para poder hacer fade
        skyDay.SetActive(true);
        skyNight.SetActive(true);

        isNight = startAtNight;

        // Estado inicial (y también fija globalAlpha de estrellas)
        SetGroupAlpha(skyDay, isNight ? 0f : 1f);
        SetGroupAlpha(skyNight, isNight ? 1f : 0f);

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

        // Remate por seguridad (deja exacto 0 o 1)
        SetGroupAlpha(skyDay, isNight ? 0f : 1f);
        SetGroupAlpha(skyNight, isNight ? 1f : 0f);

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

        // Evita división por 0
        float duration = Mathf.Max(0.01f, fadeSeconds);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);


            SetGroupAlpha(skyDay, Mathf.Lerp(dayFrom, dayTo, k));
            SetGroupAlpha(skyNight, Mathf.Lerp(nightFrom, nightTo, k));

            yield return null;
        }

        // Remate final del fade
        SetGroupAlpha(skyDay, dayTo);
        SetGroupAlpha(skyNight, nightTo);
    }

    void SetGroupAlpha(GameObject root, float a)
    {
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
            if (root == skyNight) IsNightNormalized = a;
        }
    }

    float GetAnyAlpha(GameObject root)
    {
        var sr = root.GetComponentInChildren<SpriteRenderer>(true);
        return sr ? sr.color.a : 1f;
    }
}
