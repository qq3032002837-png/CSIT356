using UnityEngine;

/// <summary>Drives in-place humanoid motion from the existing motor's actual movement.</summary>
[RequireComponent(typeof(PlayerMotor))]
public sealed class PlayerLocomotionAnimator : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    [SerializeField, Min(0f)] private float blendTime = 0.12f;
    private PlayerMotor motor;
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Running = Animator.StringToHash("Running");

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("Player locomotion needs an Animator with the Camp locomotion controller.", this);
            enabled = false;
            return;
        }
        characterAnimator.applyRootMotion = false;
    }

    private void LateUpdate()
    {
        Vector3 local = transform.InverseTransformDirection(motor.PlanarVelocity);
        float speed = new Vector2(local.x, local.z).magnitude;
        // Keep the last direction when stopping, so the exit blend does not pivot forward.
        if (speed > 0.05f)
        {
            characterAnimator.SetFloat(MoveX, local.x / speed, blendTime, Time.deltaTime);
            characterAnimator.SetFloat(MoveY, local.z / speed, blendTime, Time.deltaTime);
        }
        characterAnimator.SetFloat(Speed, speed, blendTime, Time.deltaTime);
        characterAnimator.SetBool(Running, motor.IsRunning && speed > 0.05f);
    }

    private void OnDisable()
    {
        if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null) return;
        characterAnimator.SetFloat(Speed, 0f);
        characterAnimator.SetBool(Running, false);
    }
}
