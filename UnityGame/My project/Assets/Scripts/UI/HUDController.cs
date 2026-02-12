using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement2D player;

    [Header("UI - Votos (vida)")]
    public Image votesFill;
    public int maxVotos = 10;

    [Header("UI - Fondos robados")]
    public Image fundsFill;
    public int maxFondos = 10;

    [Header("UI - Munición")]
    public TMP_Text ammoText;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement2D>();
    }

    void Update()
    {
        if (player == null) return;

        // --- VOTOS ---
        float votos01 = maxVotos <= 0 ? 0f : Mathf.Clamp01((float)player.votos / maxVotos);
        if (votesFill != null)
            votesFill.fillAmount = votos01;

        // --- FONDOS ROBADOS ---
        float fondos01 = maxFondos <= 0 ? 0f : Mathf.Clamp01((float)player.maletines / maxFondos);
        if (fundsFill != null)
            fundsFill.fillAmount = fondos01;

        // --- MUNCIÓN ---
        if (ammoText != null)
            ammoText.text = $"{player.ammoInMag}/{player.magSize} | {player.ammoReserve}";
    }
}
