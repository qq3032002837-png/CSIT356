using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCrouch : MonoBehaviour
{
    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float transitionTime = 0.2f;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float crouchCameraY = 0.15f;
    [SerializeField] private LayerMask ceilingLayers = ~0;

    public bool IsCrouching { get; private set; }
    public float CrouchSpeed { get { return crouchSpeed; } }

    private CharacterController controller;
    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 standingCameraPosition;
    private readonly Collider[] clearanceHits = new Collider[32];

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (playerCamera != null)
            standingCameraPosition = playerCamera.localPosition;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool holdingCrouch = keyboard != null &&
            (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);

        // A low ceiling prevents standing up after Ctrl is released.
        IsCrouching = holdingCrouch || !CanStand();

        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        float heightStep = Mathf.Abs(standingHeight - crouchHeight) /
            Mathf.Max(transitionTime, 0.01f) * Time.deltaTime;

        controller.height = Mathf.MoveTowards(controller.height, targetHeight, heightStep);
        // Keep the bottom of the CharacterController at the original foot position.
        controller.center = standingCenter + Vector3.up *
            ((controller.height - standingHeight) * 0.5f);

        if (playerCamera != null)
        {
            Vector3 targetCameraPosition = IsCrouching
                ? new Vector3(standingCameraPosition.x, crouchCameraY, standingCameraPosition.z)
                : standingCameraPosition;

            float cameraStep = Mathf.Abs(standingCameraPosition.y - crouchCameraY) /
                Mathf.Max(transitionTime, 0.01f) * Time.deltaTime;
            playerCamera.localPosition = Vector3.MoveTowards(
                playerCamera.localPosition, targetCameraPosition, cameraStep);
        }
    }

    private bool CanStand()
    {
        // No need for a clearance check while already standing.
        if (controller.height >= standingHeight - 0.01f)
            return true;

        float radius = Mathf.Max(0.01f, controller.radius - 0.03f);
        Vector3 center = transform.TransformPoint(standingCenter);
        float halfSegment = Mathf.Max(0f, standingHeight * 0.5f - radius);
        Vector3 bottom = center - transform.up * halfSegment;
        Vector3 top = center + transform.up * halfSegment;

        int count = Physics.OverlapCapsuleNonAlloc(
            bottom, top, radius, clearanceHits, ceilingLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = clearanceHits[i];
            if (hit != null && !hit.transform.IsChildOf(transform))
                return false;
        }

        // A full buffer cannot guarantee there are no other blocking colliders.
        return count < clearanceHits.Length;
    }
}
