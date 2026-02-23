using System.Collections;
using UnityEngine;

public class CutsceneController : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement2D player;
    public DialogueBubbleUI ui;

    [Header("Acción al terminar (solo para una conversación concreta)")]
    public CutsceneSequence onlyForThisSequence;  // arrastra aquí la conversación (asset)
    public GameObject objectToDisable;            // arrastra aquí el objeto de escena (Manifestacion (1))

    [Header("Salir a EndScene (opcional)")]
    [Tooltip("Si se asigna y la sequence es 'onlyForThisSequence', al terminar hará fade y cargará EndScene.")]
    public ScreenFaderLoader endSceneFader;

    bool isPlaying = false;

    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerMovement2D>();
        if (!ui) ui = FindFirstObjectByType<DialogueBubbleUI>(FindObjectsInactive.Include);

        Debug.Log("[CutsceneController] UI = " + (ui ? ui.name : "NULL"));
    }

    public void Play(CutsceneSequence sequence)
    {
        if (isPlaying) return;

        if (sequence == null)
        {
            Debug.LogWarning("[CutsceneController] Sequence es NULL");
            return;
        }

        Debug.Log($"[CutsceneController] Play() sequence={sequence.name} lines={(sequence.lines == null ? "NULL" : sequence.lines.Length.ToString())}");

        StartCoroutine(PlayRoutine(sequence));
    }

    IEnumerator PlayRoutine(CutsceneSequence sequence)
    {
        isPlaying = true;

        // 🔹 Forzar Idle antes de bloquear control
        if (player)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            var anim = player.animator != null ? player.animator : player.GetComponentInChildren<Animator>();
            if (anim)
            {
                anim.SetFloat("Speed", 0f); // ajusta si tu parámetro se llama distinto
            }
        }

        // 🔹 Bloquear control jugador
        if (player) player.enabled = false;

        if (ui != null) ui.Hide();

        if (sequence.lines == null || sequence.lines.Length == 0)
        {
            Debug.LogWarning("[CutsceneController] La sequence no tiene líneas.");
        }

        foreach (var line in sequence.lines)
        {
            if (ui != null)
            {
                ui.ShowLine(line.speakerName, line.text, line.speakerTransform, line.portrait);
                yield return ui.WaitForNext();
            }
            else
            {
                Debug.Log($"[CUTSCENE] {line.speakerName}: {line.text}");
                yield return new WaitForSeconds(1.2f);
            }
        }

        if (ui != null) ui.Hide();

        // 🔥 Solo si es ESTA conversación concreta
        if (sequence == onlyForThisSequence)
        {
            if (objectToDisable != null)
                objectToDisable.SetActive(false);

            // ✅ Si hay fader asignado, en vez de reactivar control y seguir, salimos a EndScene
            if (endSceneFader != null)
            {
                endSceneFader.GoToEndScene();
                isPlaying = false;
                yield break;
            }
        }

        // 🔹 Desbloquear control (comportamiento original)
        if (player) player.enabled = true;

        isPlaying = false;
    }
}