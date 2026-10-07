using UnityEngine;
using UnityEngine.InputSystem;

namespace Bufe
{
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
        [Tooltip("Camera child. Gets pitch only.")]
        public Transform cameraTransform;

        [Header("Sensitivity")]
        [Tooltip("Mouse sensitivity.")]
        public float sensitivity = 0.15f;

        /// <summary>PlayerPrefs key used to persist the player's chosen look sensitivity.</summary>
        public const string SensitivityPrefKey = "LookSensitivity";

        [Header("Pitch Clamp")]
        [Tooltip("Min pitch (degrees, negative = down).")]
        public float pitchMin = -80f;

        [Tooltip("Max pitch (degrees, positive = up).")]
        public float pitchMax = 80f;

        // ── Runtime state ────────────────────────────────────────────────────────

        private float _pitch; // camera's current vertical tilt

        // Constrained look (e.g. while operating the cash register): yaw is clamped to a
        // small range around the facing direction captured when the constraint began.
        // Pitch stays on the normal clamp so the player can still look up/down freely.
        private bool  _constrained;
        private float _yawCenter;
        private float _yawOffset;
        private float _conYawRange;

        /// <summary>
        /// Set to true by external systems (e.g. OrderManager) when a UI panel is open.
        /// While true the cursor is freed and look input is suppressed. Setting it back to
        /// false immediately re-locks the cursor and resumes look - no extra click needed.
        /// </summary>
        private bool _uiMode;
        public bool UIMode
        {
            get => _uiMode;
            set
            {
                if (_uiMode == value) return;
                _uiMode = value;

                if (_uiMode)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible   = true;
                }
                else
                {
                    LockCursor();   // resume gameplay right away - no click required
                }
            }
        }

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
                    Log.Info("[PlayerLook] Disabled PlayerCameraRotation on the camera - " +
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

            // Restore the player's saved look sensitivity (set via the pause-menu slider).
            if (PlayerPrefs.HasKey(SensitivityPrefKey))
                sensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey);

            LockCursor();
        }

        private void Update()
        {
            // ── UI mode: a menu/panel is open ────────────────────────────────────
            // Force cursor visible so the player can click UI buttons.
            // Skip ALL look input and the auto-relock check - without this guard
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

            Mouse mouse = Mouse.current;

            // Escape is handled by PauseMenuController, which opens the pause menu and
            // sets UIMode (freeing the cursor). This script no longer intercepts it.

            // ── Click → re-lock cursor (after window focus loss) ─────────────────
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    LockCursor();
                return;
            }

            if (mouse == null) return;

            // ── Look ─────────────────────────────────────────────────────────────
            Vector2 delta = mouse.delta.ReadValue() * sensitivity;

            if (_constrained)
            {
                // Yaw clamped to a small cone around the captured facing direction.
                _yawOffset = Mathf.Clamp(_yawOffset + delta.x, -_conYawRange, _conYawRange);
                transform.rotation = Quaternion.Euler(0f, _yawCenter + _yawOffset, 0f);
            }
            else
            {
                // Yaw: rotate the whole Player body left/right (free).
                transform.Rotate(Vector3.up, delta.x, Space.World);
            }

            // Pitch: tilt the camera up/down, clamped the same in both modes (kept free so
            // the player can still aim down at a keypad / up at a screen while constrained).
            _pitch -= delta.y;
            _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        /// <summary>
        /// Limits look to a small cone around the current facing direction - used while the
        /// player operates a fixed station (e.g. the cash register), so they can glance across
        /// the keypad but not turn away. Call <see cref="ClearLookConstraint"/> to release.
        /// </summary>
        public void BeginLookConstraint(float yawRange)
        {
            _constrained = true;
            _yawCenter   = transform.eulerAngles.y;
            _yawOffset   = 0f;
            _conYawRange = Mathf.Abs(yawRange);
        }

        /// <summary>Releases the look constraint set by <see cref="BeginLookConstraint"/>.</summary>
        public void ClearLookConstraint() => _constrained = false;

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

        /// <summary>
        /// Sets the look sensitivity and persists it so it survives scene loads and restarts.
        /// Called by the pause-menu sensitivity slider.
        /// </summary>
        public void SetSensitivity(float value)
        {
            sensitivity = Mathf.Max(0f, value);
            PlayerPrefs.SetFloat(SensitivityPrefKey, sensitivity);
            PlayerPrefs.Save();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
