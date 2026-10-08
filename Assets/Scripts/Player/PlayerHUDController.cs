using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays live player health and stamina using the existing PlayerStats component.
/// Attach this component to the PlayerHUD Canvas and assign its references in the Inspector.
/// </summary>
public class PlayerHUDController : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Health UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("Stamina UI")]
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TMP_Text staminaText;

    private void Awake()
    {
        // Find the player only when the reference has not been assigned manually.
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        // UI sliders are displays, not controls the player can drag.
        if (healthBar != null) healthBar.interactable = false;
        if (staminaBar != null) staminaBar.interactable = false;
    }

    private void Start()
    {
        // Populate the HUD immediately when the scene starts.
        RefreshHUD();
    }

    private void Update()
    {
        // Refresh every frame so jumps, dashes, sprinting and regeneration appear live.
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (playerStats == null) return;

        // Read health from PlayerStats without modifying gameplay values.
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = playerStats.MaxHealth;
            healthBar.value = playerStats.CurrentHealth;
        }

        if (healthText != null)
            healthText.text = $"Health: {Mathf.CeilToInt(playerStats.CurrentHealth)} / {Mathf.CeilToInt(playerStats.MaxHealth)}";

        // Read stamina from the same source used by movement and regeneration.
        if (staminaBar != null)
        {
            staminaBar.minValue = 0f;
            staminaBar.maxValue = playerStats.MaxStamina;
            staminaBar.value = playerStats.CurrentStamina;
        }

        if (staminaText != null)
            staminaText.text = $"Stamina: {Mathf.CeilToInt(playerStats.CurrentStamina)} / {Mathf.CeilToInt(playerStats.MaxStamina)}";
    }
}
