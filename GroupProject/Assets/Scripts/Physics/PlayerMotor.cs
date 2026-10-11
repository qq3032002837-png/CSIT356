using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Player movement, jumping and falling on a CharacterController.
///
/// Vertical speed is kept in a field and has gravity added to it every frame,
/// rather than being written as a flat constant. That is what makes a jump
/// possible at all: without an accumulating velocity there is nothing to carry
/// the player upward and then bring them back down.
///
/// Landing hard raises onHardLanding with the impact speed, so the level can
/// react to a fall without this script needing to know what the reaction is.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Ground speed in metres per second.")]
    [SerializeField] private float walkSpeed = 5f;

    [Tooltip("How much control the player keeps while airborne. 0 is none, 1 is full.")]
    [Range(0f, 1f)]
    [SerializeField] private float airControl = 0.4f;

    [Header("Running")]
    [SerializeField] private bool allowRunning = false;
    [SerializeField] private float runSpeed = 5f;

    public Vector3 PlanarVelocity { get; private set; }
    public bool IsRunning { get; private set; }

    [Header("Jumping")]
    [Tooltip("Downward acceleration. Stronger than real gravity so the arc feels tight rather than floaty.")]
    [SerializeField] private float gravity = -22f;
    
    private float jumpSpeed;

    [Tooltip("The max height the jump will reach. for reference, 2 is the height of the player character")]
    [SerializeField] private float jumpHeight = 3f;

    [Tooltip("1 is a normal jump. 2 allows one extra jump in mid air.")]
    [SerializeField] private int maxJumps = 1;

    [Tooltip("Grace period after walking off an edge during which a jump still works. Standard platformer forgiveness.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Falling")]
    [Tooltip("Downward speed on landing, in metres per second, above which the landing counts as hard.")]
    [SerializeField] private float hardLandingSpeed = 14f;

    [Tooltip("Raised on landing when the impact speed exceeds hardLandingSpeed. Carries that speed.")]
    public UnityEvent<float> onHardLanding;

    [Tooltip("Terminal velocity, so a long drop cannot tunnel the controller through the floor.")]
    [SerializeField] private float maxFallSpeed = 55f;

    private CharacterController controller;
    private PlayerStats stats;

    // Carried between frames. This is the whole reason jumping works.
    private float verticalVelocity;

    private int jumpsUsed;
    private float lastGroundedTime;
    private bool wasGroundedLastFrame;
    private float staminaUsage = 2.5f;

    /// <summary>True while the controller reports contact with the ground.</summary>
    public bool IsGrounded { get { return controller != null && controller.isGrounded; } }

    /// <summary>Current vertical speed. Negative is falling.</summary>
    public float VerticalVelocity { get { return verticalVelocity; } }

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<CharacterController>();
        jumpSpeed = CalculateJumpSpeed();
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        Keyboard keyboard = Keyboard.current;
        IsRunning = allowRunning && input.sqrMagnitude > 0.001f && keyboard != null
            && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        if (IsRunning) 
        {
            stats.TrySpendStamina(staminaUsage);
        }
        stats.RestoreStamina(0.5f);



        UpdateGroundState();
        HandleJumpInput();
        ApplyGravity();

        Vector3 movement = BuildMovement(input);
        Vector3 before = transform.position;
        controller.Move(movement * Time.deltaTime);
        Vector3 displacement = transform.position - before;
        displacement.y = 0f;
        PlanarVelocity = Time.deltaTime > 0f ? displacement / Time.deltaTime : Vector3.zero;
    }

    /// <summary>
    /// WASD and the arrow keys through the Input System. Read directly from the
    /// keyboard device rather than through an action asset, so this script works
    /// in any scene without one being wired up first.
    /// </summary>
    private Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) { x -= 1f; }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) { x += 1f; }
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) { y -= 1f; }
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) { y += 1f; }

        // Normalised so diagonal movement is not faster than straight movement.
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private void UpdateGroundState()
    {
        bool grounded = controller.isGrounded;

        // Landing. Checked before the jump budget is reset so the impact speed
        // is still the speed the player actually hit the ground at.
        if (grounded && !wasGroundedLastFrame)
        {
            float impactSpeed = -verticalVelocity;
            if (impactSpeed > hardLandingSpeed && onHardLanding != null)
            {
                onHardLanding.Invoke(impactSpeed);
            }
        }

        if (grounded)
        {
            lastGroundedTime = Time.time;

            // Only reset while actually falling. On the frame a jump starts,
            // isGrounded can still be true even though the player is already
            // moving upward, and resetting there would refund the jump.
            if (verticalVelocity < 0f)
            {
                jumpsUsed = 0;

                // A small downward bias keeps the controller pressed against the
                // floor, so isGrounded stays steady instead of flickering.
                verticalVelocity = -2f;
            }
        }

        wasGroundedLastFrame = grounded;
    }

    private void HandleJumpInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        // Walking off a ledge should not instantly cost the ground jump.
        bool withinCoyoteWindow = Time.time - lastGroundedTime <= coyoteTime;
        bool hasGroundJump = jumpsUsed == 0 && withinCoyoteWindow;

        if (hasGroundJump || jumpsUsed < maxJumps)
        {
            verticalVelocity = jumpSpeed;
            jumpsUsed++;
            stats.TrySpendStamina(10);
        }
    }

    private void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
    }

    private Vector3 BuildMovement(Vector2 input)
    {
        Vector3 local = new Vector3(input.x, 0f, input.y) * (IsRunning ? runSpeed : walkSpeed);

        // Local to world, so the player moves relative to the way they face.
        Vector3 world = transform.TransformDirection(local);

        if (!controller.isGrounded)
        {
            world *= airControl;
        }

        // Vertical is set after the transform, so it stays in world space and is
        // never rotated by the player's turn.
        world.y = verticalVelocity;
        return world;
    }

    /// <summary>
    /// Calculates the Jump speed necessary to achieve
    /// the desired jump height
    /// </summary>
    private float CalculateJumpSpeed()
    {
        //we will use SUVAT equations
        return Mathf.Sqrt(-2 * gravity * jumpHeight);
    }
}
