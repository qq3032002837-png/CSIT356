using UnityEngine;

/// <summary>Visual-only adapter for GhostEnemy. Does not move, damage, or destroy living ghosts.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GhostEnemy), typeof(Health))]
public class GhostAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip attackAnimation;
    [SerializeField, Min(0.05f)] private float deathDuration = 1.6f;
    [SerializeField, Min(0f)] private float deathSinkDistance = 0.6f;
    private GhostEnemy ghost;
    private Health health;
    private IState lastState;
    private float nextAttack;
    private bool waitingForFirstHit;
    private bool dying;
    private float deathElapsed;
    private Quaternion deathRotation;
    private float sinkDistance;
    private Vector3 deathPosition;

    private void Awake()
    {
        ghost = GetComponent<GhostEnemy>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        health.onDied.AddListener(BeginDeath);
    }
    private void OnDestroy()
    {
        if (health != null) health.onDied.RemoveListener(BeginDeath);
    }
    private void LateUpdate()
    {
        if (dying)
        {
            deathElapsed += Time.deltaTime;
            // Complete before the existing GhostDeadState destroys this object.
            float duration = Mathf.Max(0.01f, Mathf.Min(deathDuration, ghost.DestroyDelay - 0.05f));
            float progress = Mathf.Clamp01(deathElapsed / duration);
            // First fall backward around the character's local right axis, then sink.
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.45f));
            float sink = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - 0.45f) / 0.55f));
            transform.rotation = deathRotation * Quaternion.Euler(-90f * fall, 0f, 0f);
            transform.position = deathPosition + Vector3.down * sinkDistance * sink;
            return;
        }
        if (!ghost.enabled || ghost.CurrentState == null) return;
        IState state = ghost.CurrentState;
        if (state != lastState)
        {
            lastState = state;
            waitingForFirstHit = state == ghost.AttackState;
            nextAttack = 0f;
            if (!waitingForFirstHit) Play("Idle");
            // GhostAttackState lands its first hit on the next Update, after Enter.
            return;
        }
        if (state != ghost.AttackState) return;
        nextAttack -= Time.deltaTime;
        if (waitingForFirstHit || nextAttack <= 0f)
        {
            waitingForFirstHit = false;
            nextAttack = ghost.AttackCooldown;
            if (animator != null && attackAnimation != null)
                animator.SetFloat("AttackSpeed", Mathf.Max(1f, attackAnimation.length / Mathf.Max(0.05f, ghost.AttackCooldown - 0.1f)));
            Play("Attack");
        }
    }
    private void Play(string state)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int id = Animator.StringToHash("Base Layer." + state);
        if (animator.HasState(0, id)) animator.CrossFadeInFixedTime(id, 0.06f, 0, 0f);
    }
    private void BeginDeath()
    {
        if (dying) return;
        dying = true;
        deathRotation = transform.rotation;
        deathPosition = transform.position;
        // Freeze the current bone pose; the visual fall does not alter GhostEnemy.
        if (animator != null) animator.enabled = false;

        float groundY = deathPosition.y;
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(deathPosition + Vector3.up * 0.1f,
                     Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.distance < nearest) { nearest = hit.distance; groundY = hit.point.y; }
        }
        // A sphere around the renderers also encloses the model after it rotates.
        // Sink far enough to hide the whole body, including models floating above the floor.
        float radius = 0f;
        foreach (Renderer part in GetComponentsInChildren<Renderer>())
            radius = Mathf.Max(radius, Vector3.Distance(part.bounds.center, deathPosition) + part.bounds.extents.magnitude);
        sinkDistance = Mathf.Max(deathSinkDistance, Mathf.Max(0f, deathPosition.y - groundY) + radius + 0.2f);
    }
}
