using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float currentHealth = 100f;

    [Header("Stamina")]
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField, Min(0f)] private float currentStamina = 100f;

    [Header("Progression")]
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0)] private int experience = 0;

    [Header("Attributes")]
    [SerializeField, Min(0)] private int strength = 10;
    [SerializeField, Min(0)] private int dexterity = 10;
    [SerializeField, Min(0)] private int vitality = 10;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public int Level => level;
    public int Experience => experience;
    public int Strength => strength;
    public int Dexterity => dexterity;
    public int Vitality => vitality;

    private void Awake()
    {
        // Keep current resource values within the configured maximums.
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    public void TakeDamage(float amount)
    {
        // Ignore negative or zero damage and prevent health from falling below zero.
        if (amount <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
    }

    public void Heal(float amount)
    {
        // Restore health without exceeding the maximum health value.
        if (amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public bool TrySpendStamina(float amount)
    {
        // Reject invalid costs and actions that require more stamina than available.
        if (amount < 0f || currentStamina < amount) return false;
        currentStamina -= amount;
        return true;
    }

    public void RestoreStamina(float amount)
    {
        // Regenerate stamina without exceeding its maximum value.
        if (amount <= 0f) return;
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
    }

    public void AddExperience(int amount)
    {
        // Store earned experience; level-up rules will be added in a later step.
        if (amount <= 0) return;
        experience += amount;
    }
}
