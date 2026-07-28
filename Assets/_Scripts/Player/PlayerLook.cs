using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person mouse-look controller for the büfe player.
///
/// Horizontal (yaw)  → rotates the Player root transform, so movement always
///                     stays aligned with the camera direction.
/// Vertical  (pitch) → tilts only the camera child, clamped between pitchMin
///                     and pitchMax to prevent over-rotation.
///
/// Cursor management:
///   - Cursor is locked automatically on Start.
///   - Press Escape to release the cursor (e.g. to access menus).
///   - Click anywhere in the game window to re-lock.
///
/// Script execution order:
///   This script is set to execute AFTER default order ([DefaultExecutionOrder(10)])
///   so that PlayerHand processes input while the cursor is still in its
///   previous state, preventing a re-lock click from triggering an unwanted
///   item interaction.
///
/// Conflicts:
///   PlayerCameraRotation on the Main Camera will conflict with this script's
///   yaw control. It is disabled automatically in Start() if found.
///
/// Setup:
///   1. Attach to the Player root GameObject.
///   2. Assign 'cameraTransform' to the Main Camera child.
///   3. Adjust 'sensitivity', 'pitchMin', and 'pitchMax' in the Inspector.
///   4. Pair with PlayerMovement (also on the Player root) for WASD movement.
/// </summary>
[DefaultExecutionOrder(10)]
public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Main Camera child transform. Receives vertical (pitch) rotation only.")]
    public Transform cameraTransform;

    [Header("Sensitivity")]
    [Tooltip("Mouse movement multiplier. Adjust to taste (default 0.15).")]
    public float sensitivity = 0.15f;

    [Header("Pitch Clamp")]
    [Tooltip("Maximum downward angle (degrees). Negative = below horizon.")]
    public float pitchMin = -80f;

    [Tooltip("Maximum upward angle (degrees). Positive = above horizon.")]
    public float pitchMax = 80f;

    // ── Runtime state ────────────────────────────────────────────────────────

    private float _pitch; // camera's current vertical tilt

    /// <summary>
    /// Set to true by external systems (e.g. OrderManager) when a UI panel
    /// is open. While true:
    ///   - Cursor is forced unlocked so the player can click UI buttons.
    ///   - All look input and auto-relock behaviour are suppressed.
    ///   - Resetting to false immediately re-locks the cursor and resumes look.
    /// </summary>
    public bool UIMode { get; set; }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        // Disable the legacy incremental rotation system to avoid conflicts
        if (cameraTransform != null)
        {
            PlayerCameraRotation legacy = cameraTransform.GetComponent<PlayerCameraRotation>();
            if (legacy != null)
            {
                legacy.enabled = false;
                Debug.Log("[PlayerLook] Disabled PlayerCameraRotation on the camera — " +
                          "use PlayerLook for free mouse look instead.");
            }
        }

        // Initialise pitch from the camera's current local rotation so
        // authoring the prefab at an angle is respected
        if (cameraTransform != null)
        {
            _pitch = cameraTransform.localEulerAngles.x;
            if (_pitch > 180f) _pitch -= 360f; // normalise 270° → -90°
        }

        LockCursor();
    }

    private void Update()
    {
        // ── UI mode: a menu/panel is open ────────────────────────────────────
        // Force cursor visible so the player can click UI buttons.
        // Skip ALL look input and the auto-relock check — without this guard
        // the same click that opens the UI would be detected here
        // (PlayerLook runs after OrderManager due to [DefaultExecutionOrder(10)])
        // and would immediately re-lock the cursor.
        if (UIMode)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            return;
        }

        Keyboard kb    = Keyboard.current;
        Mouse    mouse = Mouse.current;

        // ── Escape → unlock cursor ───────────────────────────────────────────
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            return;
        }

        // ── Click → re-lock cursor (after Escape or window focus loss) ───────
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                LockCursor();
            return;
        }

        if (mouse == null) return;

        // ── Look ─────────────────────────────────────────────────────────────
        Vector2 delta = mouse.delta.ReadValue() * sensitivity;

        // Yaw: rotate the whole Player body left/right
        transform.Rotate(Vector3.up, delta.x, Space.World);

        // Pitch: tilt only the camera up/down, clamped
        _pitch -= delta.y;
        _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    /// <summary>
    /// Resets the camera's vertical tilt to level. Called on the day reset so the
    /// player always starts the morning looking straight ahead.
    /// </summary>
    public void ResetPitch()
    {
        _pitch = 0f;
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.identity;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
