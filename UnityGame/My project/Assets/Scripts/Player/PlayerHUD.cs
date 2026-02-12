using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement2D player;

    [Header("UI - Votos (vida)")]
    public Image votesFill;

    [Header("UI - Fondos robados")]
    public Image fundsFill;
    public int maxFondos = 10;

    [Header("UI - Munición")]
    public TMP_Text ammoText;

    void Awake()
    {
        FindPlayerIfNeeded();
    }

    void OnEnable()
    {
        FindPlayerIfNeeded();
        UpdateHUD();
    }

    void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            FindPlayerIfNeeded();

        UpdateHUD();
    }

    void FindPlayerIfNeeded()
    {
        if (player != null && player.gameObject.activeInHierarchy) return;
        player = FindFirstObjectByType<PlayerMovement2D>();
    }

    void UpdateHUD()
    {
        if (player == null) return;

        // --- VOTOS (VIDA) ---
        float maxV = Mathf.Max(1, player.maxVotos);
        float votos01 = Mathf.Clamp01(player.votos / maxV);

        if (votesFill != null)
            votesFill.fillAmount = votos01;

        // --- FONDOS ROBADOS ---
        float fondos01 = maxFondos <= 0 ? 0f : Mathf.Clamp01((float)player.maletines / maxFondos);
        if (fundsFill != null)
            fundsFill.fillAmount = fondos01;

        // --- MUNICIÓN ---
        if (ammoText != null)
            ammoText.text = $"{player.ammoInMag}/{player.magSize} | {player.ammoReserve}";
    }
}
