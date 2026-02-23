using System.Collections;
using UnityEngine;

public class BossDeathCutscene : MonoBehaviour
{
    [Header("Refs")]
    public DialogueBubbleUI ui;              // tu burbuja
    public ScreenFaderLoader fader;          // tu FadeLoader (con LoadScene)
    public Animator bossAnimator;            // animator del boss

    [Header("Diálogo final")]
    public string speakerName = "DIABLO";
    [TextArea(2, 4)] public string finalLine = "¡Esto no acaba aquí...!";
    public Sprite portrait;                  // opcional

    [Header("Animación de muerte")]
    public string dieTrigger = "Die";        // pon el trigger real de tu Animator
    public float deathAnimSeconds = 3f;

    [Header("Escena a cargar después")]
    public string returnSceneName = "Spain_Luis"; // nombre EXACTO de tu escena de mapa

    [Header("Return checkpoint (solo esta vuelta)")]
    public int returnCheckpointId = 3;

    bool running = false;

    void Awake()
    {
        if (!ui) ui = FindFirstObjectByType<DialogueBubbleUI>(FindObjectsInactive.Include);
        if (!bossAnimator) bossAnimator = GetComponentInChildren<Animator>();
    }

    // ✅ Llama a esto desde tu trigger (BossDeathConsoleTrigger -> OnBossDead)
    public void StartDeathSequence()
    {
        if (running) return;
        StartCoroutine(Routine());
    }

    // ✅ Marca el checkpoint SOLO para la próxima carga de escena
    public void MarkReturnCheckpoint3()
    {
        PlayerPrefs.SetInt("NEXT_CHECKPOINT_ID", returnCheckpointId);
        PlayerPrefs.Save();
        Debug.Log($"✅ NEXT_CHECKPOINT_ID = {returnCheckpointId}");
    }

    IEnumerator Routine()
    {
        running = true;

        // 1) Opcional: parar rigidbody del boss
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        // 2) Mostrar diálogo
        if (ui != null)
        {
            ui.ShowLine(speakerName, finalLine, transform, portrait);
            yield return ui.WaitForNext();
            ui.Hide();
        }

        // 3) Lanzar animación de muerte
        if (bossAnimator != null && !string.IsNullOrEmpty(dieTrigger))
            bossAnimator.SetTrigger(dieTrigger);

        // 4) Esperar la animación
        yield return new WaitForSeconds(deathAnimSeconds);

        // 5) 🔑 Marcar checkpoint de retorno (solo esta vez)
        MarkReturnCheckpoint3();

        // 6) Fade + cargar escena de vuelta
        if (fader != null)
        {
            fader.LoadScene(returnSceneName);
        }
        else
        {
            // fallback
            UnityEngine.SceneManagement.SceneManager.LoadScene(returnSceneName);
        }
    }
}