using UnityEngine;

/// <summary>
/// Lets a CharacterController shove loose Rigidbody objects out of the way.
///
/// A CharacterController is kinematic. It collides with the world but does not
/// apply any force to it, so without this the player walks straight through
/// crates and barrels as if they were painted on. This is the piece that makes
/// the level feel physical rather than decorative.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class RigidbodyPusher : MonoBehaviour
{
    [Tooltip("How hard the player shoves things. Higher is heavier.")]
    [SerializeField] private float pushForce = 2.5f;

    [Tooltip("Objects on these layers can be pushed. Leave as everything unless the level needs exceptions.")]
    [SerializeField] private LayerMask pushableLayers = ~0;

    [Tooltip("Stops the player shoving objects downwards into the floor when walking over them.")]
    [SerializeField] private bool ignoreDownwardPushes = true;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // Nothing to push: static geometry, or something driven by animation.
        if (body == null || body.isKinematic)
        {
            return;
        }

        if ((pushableLayers.value & (1 << hit.gameObject.layer)) == 0)
        {
            return;
        }

        Vector3 direction = hit.moveDirection;

        if (ignoreDownwardPushes && direction.y < -0.3f)
        {
            return;
        }

        // Flattened, so walking into a crate slides it along the floor rather
        // than driving it into the ground or launching it.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        // Applied at the contact point so an off-centre shove also spins the
        // object, which reads far better than everything sliding square on.
        body.AddForceAtPosition(direction.normalized * pushForce, hit.point, ForceMode.Impulse);
    }
}
