using UnityEngine;

public class EventTriggerZone : MonoBehaviour
{
    [Header("Qué evento lanzar")]
    public CutsceneController cutscene;   // arrástralo desde la escena
    public CutsceneSequence sequence;     // arrastra el asset del evento (lo crearemos después)

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

        if (cutscene == null || sequence == null)
        {
            Debug.LogWarning("[EventTriggerZone] Falta cutscene o sequence en el Inspector.");
            return;
        }

        Debug.Log("[EventTriggerZone] Voy a reproducir cutscene ✅");
        cutscene.Play(sequence);

        if (triggerOnce)
            fired = true;
    }
}