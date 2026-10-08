using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Hit points for anything that can be hurt: enemies now, the player later.
///
/// Whatever deals damage only needs to find this component and call
/// TakeDamage. It does not need to know whether it hit a ghost, a zombie or
/// the player, so new enemy types work with the shotgun for free.
/// </summary>
public class Health : MonoBehaviour
{
    [Tooltip("Hit points at full health.")]
    [SerializeField] private float maxHealth = 30f;

    [Tooltip("Raised after every hit. Carries the health left. A health bar can listen to this.")]
    public UnityEvent<float> onDamaged = new UnityEvent<float>();

    [Tooltip("Raised once, on the hit that takes health to zero.")]
    public UnityEvent onDied = new UnityEvent();

    public float Current { get; private set; }
    public float Max => maxHealth;
    public bool IsDead => Current <= 0f;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        // A dead thing cannot die twice, and negative damage would be healing.
        if (IsDead || amount <= 0f)
        {
            return;
        }

        Current = Mathf.Max(Current - amount, 0f);
        onDamaged?.Invoke(Current);

        if (IsDead)
        {
            onDied?.Invoke();
        }
    }
}
