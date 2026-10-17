using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Melee: an elbow strike with the shotgun in hand, at whatever is right in front of you.
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

    [Header("Elbow swing animation")]
    [Tooltip("Where the gun swings to, relative to where it is held (right, up, forward) in metres.")]
    [SerializeField] private Vector3 swingOffset = new Vector3(-0.3f, 0.05f, 0.1f);
    [Tooltip("How far the gun turns during the swing (pitch, yaw, roll) in degrees.")]
    [SerializeField] private Vector3 swingTurn = new Vector3(0f, -40f, 30f);
    [SerializeField] private float swingOutTime = 0.07f;
    [SerializeField] private float swingBackTime = 0.18f;

    private Camera cam;
    private Shotgun shotgun;
    private float nextBashTime;
    private bool isSwinging;

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

        if (shotgun != null && shotgun.HeldGun != null && !isSwinging)
        {
            StartCoroutine(ElbowSwing(shotgun.HeldGun));
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

    // The gun sweeps left and rolls, like throwing your right elbow with it in hand.
    private IEnumerator ElbowSwing(Transform gun)
    {
        isSwinging = true;
        Vector3 restPos = gun.localPosition;
        Quaternion restRot = gun.localRotation;
        Vector3 hitPos = restPos + swingOffset;
        Quaternion hitRot = restRot * Quaternion.Euler(swingTurn);

        yield return Swing(gun, restPos, restRot, hitPos, hitRot, swingOutTime);
        yield return Swing(gun, hitPos, hitRot, restPos, restRot, swingBackTime);

        gun.localPosition = restPos;
        gun.localRotation = restRot;
        isSwinging = false;
    }

    private static IEnumerator Swing(Transform gun, Vector3 fromPos, Quaternion fromRot,
        Vector3 toPos, Quaternion toRot, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            gun.localPosition = Vector3.Lerp(fromPos, toPos, k);
            gun.localRotation = Quaternion.Slerp(fromRot, toRot, k);
            yield return null;
        }
    }
}
