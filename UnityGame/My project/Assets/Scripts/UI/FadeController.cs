using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    public Image fadeImage;

    void Awake()
    {
        if (!fadeImage)
        {
            Debug.LogError("FadeController: asigna FadeImage en el Inspector.");
            enabled = false;
            return;
        }
        SetAlpha(0f);
    }

    public IEnumerator Fade(float from, float to, float seconds)
    {
        seconds = Mathf.Max(0.01f, seconds);
        float t = 0f;

        SetAlpha(from);

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            SetAlpha(Mathf.Lerp(from, to, k));
            yield return null;
        }

        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
