using UnityEngine;
using UnityEngine.Events;

public class BossDeathConsoleTrigger : MonoBehaviour
{
    [Header("Texto exacto que aparece en consola (prefijo)")]
    public string bossHpPrefix = "Enemy_BOSS HP:";

    [Header("Disparar cuando HP <= este valor")]
    public int triggerAtOrBelow = 0;

    [Header("Evento a lanzar (cutscene/fade/cargar escena)")]
    public UnityEvent onBossDead;

    bool fired;

    void OnEnable()
    {
        Application.logMessageReceived += OnLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (fired) return;

        // Queremos: "Enemy_BOSS HP: 0"
        if (string.IsNullOrEmpty(condition)) return;
        if (!condition.StartsWith(bossHpPrefix)) return;

        // Extraer número
        string numStr = condition.Substring(bossHpPrefix.Length).Trim();

        // Por si tu log viene con algo raro, intentamos parsear int
        if (!int.TryParse(numStr, out int hp)) return;

        if (hp <= triggerAtOrBelow)
        {
            fired = true;
            onBossDead?.Invoke();
        }
    }
}