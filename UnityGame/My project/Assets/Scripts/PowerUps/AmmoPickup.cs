using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo")]
    public int amount = 6;

    [Header("SFX")]
    public AudioSource sfx;     // AudioSource en este mismo objeto
    public bool destroyAfterSound = true;

    bool picked = false;

    private void Reset()
    {
        // Autorellena si hay AudioSource en el objeto
        sfx = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (picked) return;
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        picked = true;

        // Dar munición
        player.RecargarMunicionExtra(amount);

        // Sonar y destruir
        if (sfx != null && sfx.clip != null)
        {
            sfx.Play();

            if (destroyAfterSound)
            {
                // Oculta visual y desactiva colisiones para que no se recoja 2 veces
                var col = GetComponent<Collider2D>();
                if (col) col.enabled = false;

                foreach (var r in GetComponentsInChildren<Renderer>())
                    r.enabled = false;

                Destroy(gameObject, sfx.clip.length);
                return;
            }
        }

        Destroy(gameObject);
    }
}