using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFaderLoader : MonoBehaviour
{
    [Header("UI")]
    public Image fadeImage;

    [Header("Transición")]
    public float fadeOutSeconds = 0.8f;
    public float fadeInSeconds = 0.3f;
    public string sceneToLoad = "EndScene";

    bool busy = false;

    void Awake()
    {
        if (!fadeImage) fadeImage = GetComponent<Image>();
        SetAlpha(0f);
    }

    public void GoToEndScene()
    {
        if (busy) return;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        busy = true;

        // Fade OUT a negro
        yield return Fade(0f, 1f, fadeOutSeconds);

        // Cargar escena
        yield return SceneManager.LoadSceneAsync(sceneToLoad);

        // (Opcional) Fade IN desde negro
        // Si no quieres fade-in, borra estas 2 líneas
        yield return null; // deja respirar 1 frame
        yield return Fade(1f, 0f, fadeInSeconds);

        busy = false;
    }

    IEnumerator Fade(float from, float to, float seconds)
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