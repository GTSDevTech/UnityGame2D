using UnityEngine;

public class EventTriggerZone : MonoBehaviour
{
    public enum TriggerAction
    {
        PlayCutscene,
        FadeToEndScene
    }

    [Header("Qué hace este trigger")]
    public TriggerAction action = TriggerAction.PlayCutscene;

    [Header("CUTSCENE (solo si action = PlayCutscene)")]
    public CutsceneController cutscene;   // arrástralo desde la escena
    public CutsceneSequence sequence;     // arrastra el asset del evento

    [Header("END SCENE (solo si action = FadeToEndScene)")]
    public ScreenFaderLoader endSceneFader; // arrastra ScreenFader (el que tiene ScreenFaderLoader)

    [Header("Opciones")]
    public bool triggerOnce = true;

    bool fired = false;

    void Reset()
    {
        // Asegura trigger
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[EventTriggerZone] ENTER -> " + other.name + " tag=" + other.tag);

        if (fired) return;
        if (!other.CompareTag("Player")) return;

        Debug.Log("[EventTriggerZone] Es el Player ✅");

        if (action == TriggerAction.PlayCutscene)
        {
            if (cutscene == null || sequence == null)
            {
                Debug.LogWarning("[EventTriggerZone] Falta cutscene o sequence en el Inspector.");
                return;
            }

            Debug.Log("[EventTriggerZone] Voy a reproducir cutscene ✅");
            cutscene.Play(sequence);
        }
        else // FadeToEndScene
        {
            if (endSceneFader == null)
            {
                endSceneFader = FindObjectOfType<ScreenFaderLoader>();
            }

            if (endSceneFader == null)
            {
                Debug.LogWarning("[EventTriggerZone] No encuentro ScreenFaderLoader en la escena.");
                return;
            }

            Debug.Log("[EventTriggerZone] Fade -> EndScene ✅");
            endSceneFader.GoToEndScene();
        }

        if (triggerOnce)
            fired = true;
    }
}