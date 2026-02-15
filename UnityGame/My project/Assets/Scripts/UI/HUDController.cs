using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement2D player;
    public PlayerHealth playerHealth;

    [Header("UI - Vida")]
    public Image votesFill;

    [Header("UI - Maletines")]
    public Image maletinesFill;
    public TMP_Text maletinesText;

    [Header("UI - Munición")]
    public TMP_Text ammoText;

    int lastMaletines = -1;

    void Awake()
    {
        // Forzar inicio vacío
        if (maletinesFill) maletinesFill.fillAmount = 0f;
        if (maletinesText) maletinesText.text = "0/0";
    }

    void Start()
    {
        FindRefs();
        UpdateHUD();
    }

    void Update()
    {
        if (!player || !player.gameObject.activeInHierarchy || !playerHealth)
            FindRefs();

        UpdateHUD();
    }

    void FindRefs()
    {
        if (!player)
            player = FindFirstObjectByType<PlayerMovement2D>();

        if (!playerHealth)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void UpdateHUD()
    {
        if (!player || !playerHealth) return;

        // VIDA
        if (votesFill)
            votesFill.fillAmount = playerHealth.Health01;

        // MALETINES
        float maxM = Mathf.Max(1, player.maxMaletines);
        float fill = Mathf.Clamp01((float)player.maletines / maxM);

        if (maletinesFill)
            maletinesFill.fillAmount = fill;

        if (maletinesText)
            maletinesText.text = $"{player.maletines}/{player.maxMaletines}";

        // Debug opcional: ver si realmente cambia
        if (player.maletines != lastMaletines)
        {
            lastMaletines = player.maletines;
            Debug.Log($"[HUD] Maletines HUD = {player.maletines}/{player.maxMaletines} (fill={fill})");
        }

        // MUNICIÓN
        if (ammoText)
            ammoText.text = $"{player.ammoInMag}/{player.magSize} | {player.ammoReserve}";
    }
}
