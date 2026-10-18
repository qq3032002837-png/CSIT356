using UnityEngine;

/// <summary>
/// One place where an explosion is turned into forces on the world.
/// Anything that explodes (grenade, barrel, trap) calls Detonate instead of
/// duplicating the overlap-and-push logic.
/// </summary>
public static class Explosion
{
    // Lifts debris off the ground so it arcs instead of sliding flat.
    private const float DefaultUpwardsModifier = 0.5f;

    // Physics.OverlapSphereNonAlloc needs a fixed buffer. 64 is well above the
    // number of loose objects we expect in one dungeon room.
    private const int MaxAffectedColliders = 64;

    private static readonly Collider[] Hits = new Collider[MaxAffectedColliders];

    /// <param name="origin">World point of the blast.</param>
    /// <param name="radius">How far the blast reaches, in metres.</param>
    /// <param name="force">Peak force at the centre. Unity falls this off linearly to zero at the edge.</param>
    /// <param name="affects">Which layers the blast can touch.</param>
    public static void Detonate(Vector3 origin, float radius, float force, LayerMask affects)
    {
        GameAudio.PlayExplosion(GameAudio.Explosion, origin);

        int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(origin, radius, Hits, affects);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = Hits[i];
            if (hit == null)
            {
                continue;
            }

            // Walls shatter first, so their debris exists in time to be pushed.
            if (hit.TryGetComponent(out Breakable breakable))
            {
                breakable.Shatter(origin, radius, force);
                continue;
            }

            if (hit.attachedRigidbody != null)
            {
                hit.attachedRigidbody.AddExplosionForce(force, origin, radius, DefaultUpwardsModifier, ForceMode.Impulse);
            }
        }
    }
}
