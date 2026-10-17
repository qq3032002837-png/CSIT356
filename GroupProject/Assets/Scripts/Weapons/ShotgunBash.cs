using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Melee: hit whatever is right in front of you with the butt of the shotgun.
///
/// Press V. Anything with a Health component within reach takes damage and is
/// knocked back a little, so a zombie in your face can be pushed off without
/// spending a shell. Walls in the way block the hit.
///
/// The Shotgun adds this component to itself, so no scene or prefab changes.
/// </summary>
public class ShotgunBash : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [Tooltip("How far in front of the camera the bash reaches, in metres.")]
    [SerializeField] private float reach = 2f;
    [Tooltip("How wide the bash is. Bigger is more forgiving to aim.")]
    [SerializeField] private float hitRadius = 0.4f;
    [SerializeField] private float cooldown = 0.8f;
    [Tooltip("How far a hit enemy is pushed back, in metres.")]
    [SerializeField] private float knockback = 1f;

    [Header("Jab animation")]
    [SerializeField] private float jabDistance = 0.25f;
    [SerializeField] private float jabOutTime = 0.06f;
    [SerializeField] private float jabBackTime = 0.15f;

    private Camera cam;
    private Shotgun shotgun;
    private float nextBashTime;
    private bool isJabbing;

    // Falls back to this object when there is no camera, as in the headless test.
    private Transform Eye => cam != null ? cam.transform : transform;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        shotgun = GetComponent<Shotgun>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.vKey.wasPressedThisFrame)
        {
            return;
        }

        // The cursor is only free on the death screen.
        if (Cursor.lockState != CursorLockMode.Locked || Time.time < nextBashTime)
        {
            return;
        }

        nextBashTime = Time.time + cooldown;
        Bash();
    }

    /// <summary>Does one bash. Returns what was hit, or null. Public so the test can call it.</summary>
    public Health Bash()
    {
        GameAudio.Play2D(GameAudio.MeleeSwing, 0.8f, Random.Range(0.92f, 1.08f));

        if (shotgun != null && shotgun.HeldGun != null && !isJabbing)
        {
            StartCoroutine(Jab(shotgun.HeldGun));
        }

        Health target = FindTarget();
        if (target == null)
        {
            return null;
        }

        Vector3 push = target.transform.position - Eye.position;
        push.y = 0f;
        push = push.normalized * knockback;

        target.TakeDamage(damage);
        GameAudio.Play2D(GameAudio.MeleeHit, 0.9f, Random.Range(0.92f, 1.08f));
        if (!target.IsDead)
        {
            KnockBack(target, push);
        }

        return target;
    }

    private Health FindTarget()
    {
        Transform eye = Eye;
        Transform self = transform.root;

        // Triggers are included because ghosts are made of trigger colliders.
        RaycastHit[] hits = Physics.SphereCastAll(eye.position, hitRadius, eye.forward, reach,
            ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.root == self)
            {
                continue;
            }

            Health health = hit.collider.GetComponentInParent<Health>();
            if (health != null && !health.IsDead)
            {
                return health;
            }

            // A solid wall between you and the enemy stops the hit.
            // Other triggers, like pickup zones, do not.
            if (!hit.collider.isTrigger)
            {
                return null;
            }
        }

        return null;
    }

    private static void KnockBack(Health target, Vector3 push)
    {
        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // Move keeps the zombie on the NavMesh, so it cannot be pushed into a wall.
            agent.Move(push);
            return;
        }

        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
        {
            body.AddForce(push, ForceMode.VelocityChange);
            return;
        }

        // Ghosts move by setting their position and drift through walls anyway.
        target.transform.position += push;
    }

    private IEnumerator Jab(Transform gun)
    {
        isJabbing = true;
        Vector3 rest = gun.localPosition;
        Vector3 forward = rest + Vector3.forward * jabDistance;

        yield return Slide(gun, rest, forward, jabOutTime);
        yield return Slide(gun, forward, rest, jabBackTime);

        gun.localPosition = rest;
        isJabbing = false;
    }

    private static IEnumerator Slide(Transform gun, Vector3 from, Vector3 to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            gun.localPosition = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
    }
}
