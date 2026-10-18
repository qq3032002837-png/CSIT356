using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerDash : MonoBehaviour
{
    [Header("Directional Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1.2f;

    public bool IsDashing { get; private set; }
    public Vector3 DashDirection { get; private set; }
    public Vector3 DashVelocity { get { return DashDirection * dashSpeed; } }

    private float dashTimeRemaining;
    private float nextDashAllowedTime;
    // Stamina integration: a dash only consumes stamina when it actually starts.
    [SerializeField, Min(0f)] private float dashStaminaCost = 20f;
    private PlayerStats stats;

    private void Awake()
    {
        // Stamina integration: share the existing PlayerStats resource.
        stats = GetComponent<PlayerStats>();
    }

    // Starts a ground dash. Without WASD input, dodge backward relative to the player.
    public bool TryStartDash(Vector2 movementInput, Transform playerTransform)
    {
        if (IsDashing || Time.time < nextDashAllowedTime)
            return false;

        // Stamina integration: refuse a dash when there is not enough stamina.
        if (stats != null && stats.CurrentStamina < dashStaminaCost)
            return false;

        Vector3 localDirection = movementInput.sqrMagnitude > 0.001f
            ? new Vector3(movementInput.x, 0f, movementInput.y)
            : Vector3.back;

        // Calculate the horizontal dash direction locally before assigning the Vector3 property.
        Vector3 worldDirection = playerTransform.TransformDirection(localDirection.normalized);
        worldDirection.y = 0f;
        DashDirection = worldDirection.normalized;

        // Stamina integration: charge the cost after the cooldown check succeeds.
        if (stats != null && !stats.TrySpendStamina(dashStaminaCost))
            return false;

        IsDashing = true;
        dashTimeRemaining = Mathf.Max(0.01f, dashDuration);
        nextDashAllowedTime = Time.time + dashTimeRemaining + Mathf.Max(0f, dashCooldown);
        return true;
    }

    // Called by PlayerMotor once per frame, before choosing the movement velocity.
    // Returns true exactly when the active dash finishes.
    public bool Tick(float deltaTime)
    {
        if (!IsDashing)
            return false;

        dashTimeRemaining -= deltaTime;
        if (dashTimeRemaining > 0f)
            return false;

        IsDashing = false;
        return true;
    }
}
