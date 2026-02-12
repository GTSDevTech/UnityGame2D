using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement2D player;
    public PlayerHealth playerHealth;

    [Header("UI - Votos (vida)")]
    public Image votesFill;

    [Header("UI - Fondos robados")]
    public Image fundsFill;
    public int maxFondos = 10;

    [Header("UI - Munición")]
    public TMP_Text ammoText;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement2D>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void Update()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement2D>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        // --- VIDA (PlayerHealth) ---
        if (votesFill != null && playerHealth != null)
            votesFill.fillAmount = playerHealth.Health01;

        // --- FONDOS ROBADOS ---
        if (fundsFill != null && player != null)
        {
            float fondos01 = maxFondos <= 0 ? 0f : Mathf.Clamp01((float)player.maletines / maxFondos);
            fundsFill.fillAmount = fondos01;
        }

        // --- MUNCIÓN ---
        if (ammoText != null && player != null)
            ammoText.text = $"{player.ammoInMag}/{player.magSize} | {player.ammoReserve}";
    }
}
