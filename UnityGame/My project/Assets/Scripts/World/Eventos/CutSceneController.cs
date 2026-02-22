using System.Collections;
using UnityEngine;

public class CutsceneController : MonoBehaviour
{
    public PlayerMovement2D player;
    public DialogueBubbleUI ui;

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
        // --- Forzar Idle antes de bloquear ---
        if (player)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            // si tu animator est� en el player (o en un hijo), �salo:
            var anim = player.animator != null ? player.animator : player.GetComponentInChildren<Animator>();
            if (anim)
            {
                anim.SetFloat("Speed", 0f);     // tu param suele llamarse Speed
                                                // opcional: si tienes bool de run/move, lo apagamos
                                                // anim.SetBool("IsRunning", false);
            }
        }
        // -------------------------------------
        if (player) player.enabled = false;

        // Bloquear control
        if (player) player.enabled = false;

        if (ui != null) ui.Hide();

        if (sequence.lines == null || sequence.lines.Length == 0)
        {
            Debug.LogWarning("[CutsceneController] La sequence no tiene l�neas.");
        }

        foreach (var line in sequence.lines)
        {
            Debug.Log("[CUTSCENE] Lanzando linea -> " + line.text);

            if (ui != null)
            {
                ui.ShowLine(line.speakerName, line.text, line.speakerTransform);
                yield return ui.WaitForNext();
            }
            else
            {
                Debug.Log($"[CUTSCENE] {line.speakerName}: {line.text}");
                yield return new WaitForSeconds(1.2f);
            }
        }

        if (ui != null) ui.Hide();

        // Desbloquear control
        if (player) player.enabled = true;

        isPlaying = false;
    }
}