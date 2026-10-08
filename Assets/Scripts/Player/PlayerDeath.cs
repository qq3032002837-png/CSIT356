using UnityEngine;

/// <summary>
/// Detects player death, stops player movement and displays the death screen.
/// Respawning restores resources and moves the player to a safe spawn point.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;

    // Respawn integration: use an optional checkpoint, or the initial spawn position.
    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;

    public bool IsDead { get; private set; }

    private PlayerStats stats;
    private PlayerMotor motor;
    // Respawn integration: keep the view and cursor usable during the death screen.
    private CharacterController controller;
    private MouseLook[] mouseLookComponents;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;

    private void Awake()
    {
        // Read the existing health value and movement components on the player.
        stats = GetComponent<PlayerStats>();
        motor = GetComponent<PlayerMotor>();
        // Respawn integration: cache the initial spawn and camera controls.
        controller = GetComponent<CharacterController>();
        mouseLookComponents = GetComponentsInChildren<MouseLook>(true);
        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;

        // Keep the death screen hidden until health reaches zero.
        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void Update()
    {
        // Enter the death state once; do not repeat it every frame.
        if (!IsDead && stats.CurrentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        IsDead = true;

        // Respawn integration: release the cursor so the UI button can be clicked.
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        foreach (MouseLook mouseLook in mouseLookComponents)
        {
            if (mouseLook != null)
                mouseLook.enabled = false;
        }

        // PlayerMotor drives walking, sprinting, jumping.
        if (motor != null)
        {
            motor.enabled = false;
        }

        // Show the configured YOU DIED overlay while leaving the camera active.
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PlayerDeath: Death Panel is not assigned.", this);
        }
    }

    // Respawn integration: connect this public method to the UI Button On Click event.
    public void Respawn()
    {
        if (!IsDead)
            return;

        // Disable collision during teleportation to avoid moving through obstacles.
        if (controller != null)
            controller.enabled = false;

        transform.position = respawnPoint != null ? respawnPoint.position : initialSpawnPosition;
        transform.rotation = respawnPoint != null ? respawnPoint.rotation : initialSpawnRotation;

        if (controller != null)
            controller.enabled = true;

        // Restore both resources using existing PlayerStats methods.
        stats.Heal(stats.MaxHealth);
        stats.RestoreStamina(stats.MaxStamina);

        // Restore gameplay control and hide the death overlay.
        IsDead = false;
        if (motor != null)
            motor.enabled = true;
        foreach (MouseLook mouseLook in mouseLookComponents)
        {
            if (mouseLook != null)
                mouseLook.enabled = true;
        }
        if (deathPanel != null)
            deathPanel.SetActive(false);

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;
    }
}
