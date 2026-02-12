using System.Collections;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager I;

    [Header("Start checkpoint")]
    [Tooltip("Si está activo, al iniciar se fija el checkpoint con ID más bajo encontrado (normalmente 0).")]
    public bool autoSetStartCheckpointOnPlay = true;

    [Header("Runtime")]
    public Vector3 lastCheckpointPos;
    public bool hasCheckpoint;
    public int currentCheckpointId = -1;

    [Header("Respawn timings")]
    public float fadeOutSeconds = 0.25f;
    public float holdBlackSeconds = 0.05f;
    public float fadeInSeconds = 0.25f;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        if (!autoSetStartCheckpointOnPlay) return;

        // Busca todos los checkpoints y se queda con el de ID más bajo como "inicio"
        var cps = FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);
        if (cps == null || cps.Length == 0)
        {
            Debug.LogWarning("⚠️ No hay CheckpointTrigger en la escena. No se puede setear checkpoint inicial.");
            return;
        }

        CheckpointTrigger start = cps[0];
        for (int i = 1; i < cps.Length; i++)
        {
            if (cps[i].checkpointId < start.checkpointId)
                start = cps[i];
        }

        ForceSetCheckpoint(start);
        Debug.Log($"🏁 Checkpoint inicial auto-set -> ID {currentCheckpointId} en {lastCheckpointPos}");
    }

    // ✅ SOLO acepta si el checkpoint es más avanzado que el actual
    public bool TrySetCheckpoint(CheckpointTrigger cp)
    {
        if (cp == null) return false;

        if (cp.checkpointId <= currentCheckpointId)
        {
            // No retrocede
            // Debug.Log($"⏭️ Ignorado CP ID {cp.checkpointId} (actual {currentCheckpointId})");
            return false;
        }

        ForceSetCheckpoint(cp);
        Debug.Log($"✅ Checkpoint actualizado -> ID {currentCheckpointId} en {lastCheckpointPos}");
        return true;
    }

    // ✅ Fuerza set (para checkpoint inicial, o si tú quieres permitir volver atrás)
    public void ForceSetCheckpoint(CheckpointTrigger cp)
    {
        currentCheckpointId = cp.checkpointId;
        lastCheckpointPos = cp.GetSpawnPosition();
        hasCheckpoint = true;
    }

    public void RespawnPlayer(Transform player)
    {
        if (player == null) return;

        if (!hasCheckpoint)
        {
            Debug.LogWarning("⚠️ No hay checkpoint guardado. Respawn cancelado.");
            return;
        }

        StartCoroutine(RespawnRoutine(player));
    }

    IEnumerator RespawnRoutine(Transform player)
    {
        // 1) Fade a negro (si existe FadeController)
        var fade = FindFirstObjectByType<FadeController>();
        if (fade != null)
            yield return fade.Fade(0f, 1f, fadeOutSeconds);

        // 2) Teleport al checkpoint sin “rebotes”
        var rb = player.GetComponent<Rigidbody2D>();
        var col = player.GetComponent<Collider2D>();

        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        player.position = lastCheckpointPos;

        // espera 1 frame para que Unity asiente el transform
        yield return null;

        if (rb != null) rb.simulated = true;
        if (col != null) col.enabled = true;

        // 3) Reset de vida/estado (si existe PlayerHealth)
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
            health.ResetAfterRespawn();

        // 4) Pausa mínima en negro
        if (holdBlackSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdBlackSeconds);

        // 5) Fade in
        if (fade != null)
            yield return fade.Fade(1f, 0f, fadeInSeconds);
    }
}
