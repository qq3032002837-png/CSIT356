using UnityEngine;

/// <summary>
/// Applies continuous damage to a PlayerStats component while its collider
/// remains inside this trigger volume.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [Header("Damage Zone")]
    [Tooltip("Health lost per second while the player remains in the zone.")]
    [SerializeField, Min(0f)] private float damagePerSecond = 15f;

    private void Reset()
    {
        // Configure newly added colliders as trigger volumes by default.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        // Find PlayerStats on the entering collider or its parent Player object.
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null || stats.CurrentHealth <= 0f)
            return;

        // Physics trigger callbacks use fixed-time steps for steady damage.
        stats.TakeDamage(damagePerSecond * Time.fixedDeltaTime);
    }
}
