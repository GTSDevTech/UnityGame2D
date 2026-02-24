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

    [Header("SFX (en el propio pickup)")]
    public AudioSource pickupSFX;          // AudioSource en ESTE objeto
    public bool destroyAfterSound = true;  // destruir cuando termine el sonido

    bool picked = false;

    private void Reset()
    {
        // Auto-rellena si hay AudioSource en el objeto
        pickupSFX = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (picked) return;
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        picked = true;

        // Aplica el powerup
        player.AgregarPowerUp(tipo);

        // Sonido del pickup (el que tenga este prefab)
        if (pickupSFX != null && pickupSFX.clip != null)
        {
            pickupSFX.Play();

            if (destroyAfterSound)
            {
                // Evita doble pickup y oculta el objeto mientras suena
                var col = GetComponent<Collider2D>();
                if (col) col.enabled = false;

                foreach (var r in GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;

                Destroy(gameObject, pickupSFX.clip.length);
                return;
            }
        }

        Destroy(gameObject);
    }
}