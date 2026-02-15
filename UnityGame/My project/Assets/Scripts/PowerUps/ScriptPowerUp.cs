using UnityEngine;

public enum TipoPowerUp
{
    Maletin,
    Voto,
    Municion
}

public class PowerUp : MonoBehaviour
{
    public TipoPowerUp tipo;

    private void Reset()
    {
        // Asegura trigger por defecto
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null)
        {
            Debug.LogError("[PowerUp] El Player no tiene PlayerMovement2D.", other);
            return;
        }

        int before = player.maletines;

        player.AgregarPowerUp(tipo);

        Debug.Log($"[PowerUp] {tipo} recogido. Maletines: {before} -> {player.maletines}", this);

        Destroy(gameObject);
    }
}
