using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A ground-based enemy that spots the player using sight and then pursues forever.
/// Uses NavMeshAgent to follow corridors instead of passing through walls.
/// Reuses the existing Health and StateMachine classes.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieEnemy : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Player transform. If empty, finds the object tagged Player.")]
    [SerializeField] private Transform target;

    [Header("Sight")]
    [SerializeField, Min(0.1f)] private float sightRange = 15f;
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 120f;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float targetEyeHeight = 1.0f;
    [SerializeField] private LayerMask sightLayers = ~0;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.5f;
    [SerializeField, Min(0f)] private float attackExitBuffer = 0.75f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 1.5f;

    [Header("Death")]
    [SerializeField, Min(0f)] private float destroyDelay = 2f;

    [Header("Animation")]
    [Tooltip("Animator using the ZombieChaser controller. Root motion stays disabled; NavMesh drives movement.")]
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip deathAnimation;

    private Health health;
    private NavMeshAgent agent;
    private readonly StateMachine machine = new StateMachine();

    public ZombieIdleState IdleState { get; private set; }
    public ZombieChaseState ChaseState { get; private set; }
    public ZombieAttackState AttackState { get; private set; }
    public ZombieDeadState DeadState { get; private set; }
    public IState CurrentState => machine.Current;
    public bool HasTarget => target != null;
    public float AttackRange => attackRange;
    public float AttackExitRange => attackRange + attackExitBuffer;
    public float AttackCooldown => attackCooldown;
    public float DestroyDelay => Mathf.Max(destroyDelay, deathAnimation != null ? deathAnimation.length + 0.2f : 0f);

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        IdleState = new ZombieIdleState(this);
        ChaseState = new ZombieChaseState(this);
        AttackState = new ZombieAttackState(this);
        DeadState = new ZombieDeadState(this);
        health.onDied.AddListener(OnDied);
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
            else Debug.LogWarning($"{name}: no Player target found.", this);
        }

        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name}: NavMeshAgent is not on a baked NavMesh.", this);

        machine.ChangeState(IdleState);
    }

    private void Update() => machine.Tick(Time.deltaTime);

    private void OnDestroy()
    {
        if (health != null) health.onDied.RemoveListener(OnDied);
    }

    // State names belong to the dedicated ZombieChaser controller. Existing unanimated
    // test enemies can continue using this script without an Animator.
    public void PlayAnimation(string state, float fade = 0.12f)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (animator.HasState(0, hash)) animator.CrossFadeInFixedTime(hash, fade, 0, 0f);
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    // Allow zombie states to request transitions through the shared state machine.
    public void ChangeState(IState next)
    {
        machine.ChangeState(next);
    }

    public float DistanceToTarget()
    {
        if (target == null) return float.PositiveInfinity;
        Vector3 delta = target.position - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    /// <summary>
    /// Sight is required only for the initial Idle-to-Chase transition.
    /// The nearest raycast hit must belong to the player; walls block visibility.
    /// </summary>
    public bool CanSeeTarget()
    {
        if (target == null) return false;

        Vector3 from = transform.position + Vector3.up * eyeHeight;
        Vector3 to = target.position + Vector3.up * targetEyeHeight;
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.001f || distance > sightRange) return false;
        if (Vector3.Angle(transform.forward, direction) > fieldOfView * 0.5f) return false;

        // Ignore this zombie's own colliders, but respect walls and other blockers.
        RaycastHit[] hits = Physics.RaycastAll(from, direction.normalized, distance, sightLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        return false;
    }

    public void ChaseTarget()
    {
        if (target == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    public void StopMoving()
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void FaceTarget(float deltaTime)
    {
        if (target == null) return;
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(direction), agent.angularSpeed * deltaTime);
    }

    public void HitTarget()
    {
        if (target == null) return;
        PlayAnimation("Attack", 0.05f);
        // Integration: player damage is managed by the existing PlayerStats system.
        PlayerStats stats = target.GetComponentInParent<PlayerStats>();
        if (stats != null) stats.TakeDamage(attackDamage);
    }

    private void OnDied() => machine.ChangeState(DeadState);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * eyeHeight, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
