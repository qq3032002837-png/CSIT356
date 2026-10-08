using UnityEngine;

/// <summary>
/// Regenerates the stamina stored in PlayerStats after a short delay.
/// Existing systems can continue to spend stamina through PlayerStats.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Regeneration")]
    [SerializeField, Min(0f)] private float regenerationRate = 20f;
    [SerializeField, Min(0f)] private float regenerationDelay = 1f;

    private PlayerStats stats;
    private float lastStaminaUseTime;
    private float previousStamina;

    private void Awake()
    {
        // Cache the existing stats component; do not duplicate stamina data.
        stats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        // Begin with the configured stamina amount and no regeneration delay.
        previousStamina = stats.CurrentStamina;
        lastStaminaUseTime = Time.time - regenerationDelay;
    }

    private void Update()
    {
        // Detect stamina spent by any gameplay system using PlayerStats.
        if (stats.CurrentStamina < previousStamina - 0.0001f)
        {
            lastStaminaUseTime = Time.time;
        }

        // Regenerate only after the delay and never exceed maximum stamina.
        if (Time.time >= lastStaminaUseTime + regenerationDelay &&
            stats.CurrentStamina < stats.MaxStamina)
        {
            stats.RestoreStamina(regenerationRate * Time.deltaTime);
        }

        // Save the final value to detect new stamina expenditure next frame.
        previousStamina = stats.CurrentStamina;
    }
}
