using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Order")]
    [Tooltip("Orden del checkpoint. 0 = inicio, 1 = siguiente, etc.")]
    public int checkpointId = 0;

    [Header("Spawn")]
    [Tooltip("Punto exacto donde respawnea (hijo SpawnPoint). Si es null usa la posición del propio CP.")]
    public Transform spawnPoint;

    [Tooltip("Si es true, solo se activa una vez (por seguridad).")]
    public bool oneShot = false;

    bool used;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    public Vector3 GetSpawnPosition()
    {
        return (spawnPoint != null) ? spawnPoint.position : transform.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        if (CheckpointManager.I == null)
        {
            Debug.LogError("No hay CheckpointManager en escena.");
            return;
        }

        // Pide al manager que lo acepte (solo si es más avanzado)
        bool accepted = CheckpointManager.I.TrySetCheckpoint(this);

        if (accepted && oneShot)
            used = true;
    }
}
