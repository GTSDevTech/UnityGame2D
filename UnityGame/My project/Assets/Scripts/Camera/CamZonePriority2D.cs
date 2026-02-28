using UnityEngine;
using Unity.Cinemachine;

public class CamZonePriority2D : MonoBehaviour
{
    public CinemachineCamera camEvento;
    public CinemachineCamera camGameplay;

    public int prioEvento = 20;
    public int prioGameplay = 10;

    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[CamZone] ENTER: {other.name} root:{other.transform.root.name} tagRoot:{other.transform.root.tag}");
        if (!other.transform.root.CompareTag(playerTag)) return;

        Debug.Log("[CamZone] MATCH Player → cambiando prioridades");
        camEvento.Priority = prioEvento;
        camGameplay.Priority = prioGameplay;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.transform.root.CompareTag(playerTag)) return;

        camEvento.Priority = 0;
        camGameplay.Priority = prioGameplay;
    }


}