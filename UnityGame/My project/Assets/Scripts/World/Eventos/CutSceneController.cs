using System.Collections;
using UnityEngine;

public class CutsceneController : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement2D player;
    public DialogueBubbleUI ui;

    [Header("Acción al terminar (desactivar objeto)")]
    public CutsceneSequence onlyForThisSequence;
    public GameObject objectToDisable;

    [Header("Transición con Fade (diablo)")]
    public CutsceneSequence sequenceToLoadScene;   // conversación del diablo
    public ScreenFaderLoader faderLoader;         // tu ScreenFaderLoader
    public string sceneToLoad = "Scene_Azotea";   // escena de pelea

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
        if (sequence == null) return;

        StartCoroutine(PlayRoutine(sequence));
    }

    IEnumerator PlayRoutine(CutsceneSequence sequence)
    {
        isPlaying = true;

        // 🔹 Forzar Idle
        if (player)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            var anim = player.animator != null ? player.animator : player.GetComponentInChildren<Animator>();
            if (anim) anim.SetFloat("Speed", 0f);
        }

        // 🔹 Bloquear control
        if (player) player.enabled = false;

        if (ui != null) ui.Hide();

        foreach (var line in sequence.lines)
        {
            if (ui != null)
            {
                ui.ShowLine(line.speakerName, line.text, line.speakerTransform, line.portrait);
                yield return ui.WaitForNext();
            }
        }

        if (ui != null) ui.Hide();

        // 🔥 Conversación que desactiva objeto
        if (sequence == onlyForThisSequence && objectToDisable != null)
        {
            objectToDisable.SetActive(false);
        }

        // 🔥 Conversación del DIABLO → Fade + cargar Scene_Azotea
        if (sequence == sequenceToLoadScene && faderLoader != null)
        {
            faderLoader.LoadScene(sceneToLoad);
            isPlaying = false;
            yield break;
        }

        // 🔹 Desbloquear control normal
        if (player) player.enabled = true;

        isPlaying = false;
    }
}