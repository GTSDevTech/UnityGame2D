using UnityEngine;
using UnityEngine.Playables;

public class FinalCutsceneOnBossDeath : MonoBehaviour
{
    [Header("Timeline (opcional)")]
    public PlayableDirector director;

    [Header("Animator (opcional)")]
    public Animator animator;
    public string triggerName = "PlayFinal";

    [Header("Bloqueos opcionales")]
    public GameObject playerRoot; // para desactivar input/movimiento si quieres

    bool played;

    // Esto lo llamas desde el UnityEvent onBossDied del BossHealth
    public void PlayFinalCutscene()
    {
        if (played) return;
        played = true;

        // 1) Bloquear jugador (simple y directo)
        if (playerRoot != null)
            playerRoot.SetActive(false);

        // 2) Timeline si existe
        if (director != null)
        {
            director.Play();
            return;
        }

        // 3) Animator si existe
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
            return;
        }

        Debug.LogWarning("FinalCutsceneOnBossDeath: No hay director ni animator asignado.");
    }
}