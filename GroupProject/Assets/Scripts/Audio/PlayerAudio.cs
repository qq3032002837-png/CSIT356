using UnityEngine;

/// <summary>
/// The player's own sounds: footsteps, landing, and getting hurt.
///
/// It only reads PlayerMotor and PlayerStats, so neither script had to change.
/// Footsteps follow the real ground speed, so standing still, falling or
/// walking into a wall makes no steps.
/// </summary>
public class PlayerAudio : MonoBehaviour
{
    private const float WalkStepGap = 0.5f;
    private const float RunStepGap = 0.33f;
    private const float MinStepSpeed = 0.5f;
    private const float StepVolume = 0.45f;
    private const float StepPitchVariation = 0.08f;

    // Falls slower than this, in metres per second, land silently.
    private const float MinLandingSpeed = 4f;

    private PlayerMotor motor;
    private PlayerStats stats;

    private float stepTimer;
    private bool wasGrounded = true;
    private float fastestFall;
    private float lastHealth;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        stats = GetComponent<PlayerStats>();
        lastHealth = stats != null ? stats.CurrentHealth : 0f;
    }

    private void Update()
    {
        if (motor != null)
        {
            UpdateFootsteps();
            UpdateLanding();
        }

        if (stats != null)
        {
            UpdateHurt();
        }
    }

    private void UpdateFootsteps()
    {
        bool moving = motor.IsGrounded && motor.PlanarVelocity.magnitude > MinStepSpeed;
        if (!moving)
        {
            // Ready to step as soon as movement starts again.
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f)
        {
            return;
        }

        stepTimer = motor.IsRunning ? RunStepGap : WalkStepGap;
        float pitch = 1f + Random.Range(-StepPitchVariation, StepPitchVariation);
        GameAudio.Play2D(GameAudio.Footsteps, StepVolume, pitch);
    }

    private void UpdateLanding()
    {
        bool grounded = motor.IsGrounded;

        if (!grounded)
        {
            // Remember the fastest fall, because PlayerMotor resets the
            // vertical speed on the frame it touches the ground.
            fastestFall = Mathf.Min(fastestFall, motor.VerticalVelocity);
        }
        else if (!wasGrounded)
        {
            if (-fastestFall >= MinLandingSpeed)
            {
                GameAudio.Play2D(GameAudio.Land);
            }

            fastestFall = 0f;
        }

        wasGrounded = grounded;
    }

    private void UpdateHurt()
    {
        float current = stats.CurrentHealth;

        // Health going down is a hit. Going up is healing or a respawn, which stays silent.
        if (current < lastHealth)
        {
            GameAudio.Play2D(GameAudio.Hurt);
        }

        lastHealth = current;
    }
}
