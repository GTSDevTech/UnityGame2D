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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerMovement2D>();
        if (player != null)
        {
            player.AgregarPowerUp(tipo);
        }

        Destroy(gameObject);
    }
}