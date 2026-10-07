using UnityEngine;
using UnityEngine.InputSystem;

namespace Bufe
{
    /// <summary>
    /// First-person movement controller for the büfe player.
    /// Moves the Player GameObject horizontally with WASD / arrow keys,
    /// relative to the direction the player is facing.
    /// Gravity is simulated so the player stays grounded on the floor.
    ///
    /// Setup:
    ///   1. Attach to the Player root GameObject.
    ///   2. Unity will automatically add a CharacterController (RequireComponent).
    ///      Remove any existing CapsuleCollider - CharacterController has its own.
    ///   3. Tune 'moveSpeed' and 'gravity' in the Inspector.
    ///   4. Pair with PlayerLook (also on the Player root) for mouse-look.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Move speed.")]
        public float moveSpeed = 4f;

        [Header("Physics")]
        [Tooltip("Gravity (negative).")]
        public float gravity = -9.81f;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private CharacterController _cc;
        private float               _verticalVelocity;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();

            // CharacterController provides its own internal capsule collider.
            // A separate CapsuleCollider on the same GameObject is redundant and
            // causes physics conflicts: the extra collider can trigger depenetration
            // pushes from nearby Rigidbodies that unexpectedly move the player.
            // Auto-disable it here so the scene does not need to be manually fixed.
            CapsuleCollider extra = GetComponent<CapsuleCollider>();
            if (extra != null)
            {
                extra.enabled = false;
                Log.Info("[PlayerMovement] Disabled redundant CapsuleCollider - " +
                          "CharacterController provides its own. " +
                          "Remove it from the Player GameObject to clean up.", this);
            }
        }

        private void Update()
        {
            // Only move when the cursor is locked (i.e. the game is in focus / play mode)
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // ── Horizontal input ─────────────────────────────────────────────────
            float x = 0f, z = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  z -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;

            // Move relative to the player's current facing direction
            Vector3 horizontal = transform.right * x + transform.forward * z;
            if (horizontal.sqrMagnitude > 1f) horizontal.Normalize();

            // ── Gravity ──────────────────────────────────────────────────────────
            // A small constant downward push keeps isGrounded reliable on flat floors
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            _verticalVelocity += gravity * Time.deltaTime;

            // ── Apply motion ─────────────────────────────────────────────────────
            Vector3 motion = horizontal * moveSpeed + Vector3.up * _verticalVelocity;
            _cc.Move(motion * Time.deltaTime);
        }
    }
}
