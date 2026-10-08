using UnityEngine;

/// <summary>
/// The ghost: slow, always comes for you, and goes straight through walls.
///
/// Behaviour is a state machine with four states (see GhostStates.cs):
///   Idle   -> Chase   when the player comes within detectionRange
///   Chase  -> Attack  when the player is within attackRange
///   Attack -> Chase   when the player gets further than attackRange + attackExitBuffer
///   any    -> Dead    when this ghost's Health reaches zero
/// Once it starts chasing it never gives up, so there is no hiding from it.
///
/// Movement is Kinematic Seek (Lecture 6): head straight for the target at a
/// fixed speed. It does not use a NavMesh, because a ghost that passes through
/// walls has no reason to path around them.
/// </summary>
[RequireComponent(typeof(Health))]
public class GhostEnemy : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Who the ghost hunts. Leave empty to find the object tagged Player at start.")]
    [SerializeField] private Transform target;

    [Header("Senses")]
    [Tooltip("How close, in metres, the player must come before the ghost wakes up.")]
    [SerializeField] private float detectionRange = 20f;

    [Header("Movement")]
    [Tooltip("Metres per second. Keep it slower than the player's walk speed (5) so it can be outrun.")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("Degrees per second the ghost turns to face the player.")]
    [SerializeField] private float turnSpeed = 180f;

    [Header("Attack")]
    [Tooltip("How close, in metres, the ghost must be to hurt the player.")]
    [SerializeField] private float attackRange = 1.5f;

    [Tooltip("Extra distance the player must open up before the ghost stops attacking. " +
             "Different enter and exit distances stop it flicking between Chase and Attack every frame.")]
    [SerializeField] private float attackExitBuffer = 0.75f;

    [Tooltip("Damage per hit.")]
    [SerializeField] private float attackDamage = 10f;

    [Tooltip("Seconds between hits.")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Death")]
    [Tooltip("Seconds the body stays before it is removed. Room for a death animation or sound.")]
    [SerializeField] private float destroyDelay = 2f;

    private Health health;
    private readonly StateMachine machine = new StateMachine();

    public GhostIdleState IdleState { get; private set; }
    public GhostChaseState ChaseState { get; private set; }
    public GhostAttackState AttackState { get; private set; }
    public GhostDeadState DeadState { get; private set; }

    public IState CurrentState => machine.Current;
    public bool HasTarget => target != null;
    public float DetectionRange => detectionRange;
    public float AttackRange => attackRange;
    public float AttackExitRange => attackRange + attackExitBuffer;
    public float AttackCooldown => attackCooldown;
    public float DestroyDelay => destroyDelay;

    private void Awake()
    {
        health = GetComponent<Health>();

        // Triggers are ignored by walls and by the player's CharacterController,
        // which is what lets the ghost drift through the maze. The shotgun's
        // raycasts still hit triggers, so it can still be shot.
        foreach (Collider part in GetComponentsInChildren<Collider>())
        {
            part.isTrigger = true;
        }

        IdleState = new GhostIdleState(this);
        ChaseState = new GhostChaseState(this);
        AttackState = new GhostAttackState(this);
        DeadState = new GhostDeadState(this);

        health.onDied.AddListener(OnDied);
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning($"{name} found no object tagged Player, so it will stay idle.", this);
            }
        }

        machine.ChangeState(IdleState);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDied.RemoveListener(OnDied);
        }
    }

    /// <summary>Runs one frame of behaviour. Public so a test can step the ghost by hand.</summary>
    public void Tick(float deltaTime)
    {
        machine.Tick(deltaTime);
    }

    public void ChangeState(IState next)
    {
        machine.ChangeState(next);
    }

    /// <summary>Sets who the ghost hunts. Used by spawners and tests.</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Distance to the player on the ground plane. Height is ignored, so a ghost
    /// floating above the floor still counts as close.
    /// </summary>
    public float DistanceToTarget()
    {
        if (target == null)
        {
            return float.PositiveInfinity;
        }

        return FlatOffsetToTarget().magnitude;
    }

    /// <summary>
    /// Kinematic Seek: velocity = direction to target x max speed.
    /// The step is clamped so the ghost never overshoots the player.
    /// </summary>
    public void SeekTarget(float deltaTime)
    {
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = FlatOffsetToTarget();
        float distance = toTarget.magnitude;
        if (distance < 0.001f)
        {
            return;
        }

        Vector3 velocity = toTarget / distance * moveSpeed;
        Vector3 step = Vector3.ClampMagnitude(velocity * deltaTime, distance);
        transform.position += step;

        FaceTarget(deltaTime);
    }

    public void FaceTarget(float deltaTime)
    {
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = FlatOffsetToTarget();
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion wanted = Quaternion.LookRotation(toTarget, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * deltaTime);
    }

    /// <summary>Hurts the player through the existing PlayerStats component.</summary>
    public void HitTarget()
    {
        if (target == null)
        {
            return;
        }

        // Integration: use PlayerStats as the single source of player health for the HUD and death system.
        PlayerStats targetStats = target.GetComponentInParent<PlayerStats>();
        if (targetStats != null)
        {
            targetStats.TakeDamage(attackDamage);
        }
    }

    private Vector3 FlatOffsetToTarget()
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset;
    }

    private void OnDied()
    {
        machine.ChangeState(DeadState);
    }

    // Draws the two ranges in the Scene view when the ghost is selected.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
