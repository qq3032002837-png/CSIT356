using UnityEngine;

/// <summary>
/// A wall or prop that survives until it takes a hard enough hit, then is
/// replaced by a pre-fractured version made of loose Rigidbody pieces.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Breakable : MonoBehaviour
{
    [Header("Fracture")]
    [Tooltip("Prefab of the same object split into pieces. Every piece needs its own Rigidbody and Collider.")]
    [SerializeField] private GameObject fracturedPrefab;

    [Tooltip("Seconds before the debris is cleaned up. Keeps the physics step cheap.")]
    [SerializeField] private float debrisLifetime = 8f;

    [Header("Impact")]
    [Tooltip("Collision impulse needed to break this without an explosion.")]
    [SerializeField] private float impactThreshold = 12f;

    private bool hasShattered;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude < impactThreshold)
        {
            return;
        }

        Vector3 impactPoint = collision.GetContact(0).point;
        Shatter(impactPoint, impactThreshold, collision.impulse.magnitude);
    }

    /// <summary>
    /// Swap this object for its debris and push the pieces away from the blast.
    /// Safe to call more than once; only the first call does anything.
    /// </summary>
    public void Shatter(Vector3 origin, float radius, float force)
    {
        if (hasShattered)
        {
            return;
        }

        hasShattered = true;

        if (fracturedPrefab == null)
        {
            Debug.LogWarning($"{name} has no fracturedPrefab assigned, destroying without debris.", this);
            Destroy(gameObject);
            return;
        }

        GameObject debris = Instantiate(fracturedPrefab, transform.position, transform.rotation);

        foreach (Rigidbody piece in debris.GetComponentsInChildren<Rigidbody>())
        {
            piece.AddExplosionForce(force, origin, radius, 0.5f, ForceMode.Impulse);
        }

        Destroy(debris, debrisLifetime);
        Destroy(gameObject);
    }
}
