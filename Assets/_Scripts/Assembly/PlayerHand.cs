using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Player hand controller for the physical assembly system.
/// Handles picking up, holding, and dropping/throwing InteractableItems.
///
/// The player holds the actual world object (no hand prefab) — the item's
/// own mesh is visible while parented to the holdPoint.
///
/// Input:
///   Left click  → interact with IAssemblyInteractable, or pick up an InteractableItem
///   Right click → drop/throw the held item forward
///
/// Setup:
///   1. Attach to the player GameObject.
///   2. Create an empty child under the Main Camera and assign it as holdPoint.
///   3. Optionally assign handStatusText for UI feedback.
/// </summary>
namespace Bufe
{
    public class PlayerHand : MonoBehaviour
    {
        [Header("Hold Point")]
        [Tooltip("Empty child of the Camera where held items are positioned and displayed.")]
        public Transform holdPoint;

        [Header("Input")]
        [Tooltip("Maximum raycast distance for clicking on items and stations.")]
        public float interactRange = 10f;

        [Tooltip("Forward force applied to the item when dropped/thrown.")]
        public float throwForce = 5f;

        [Header("UI")]
        [Tooltip("Optional TextMeshProUGUI showing what the player is currently holding.")]
        public TextMeshProUGUI handStatusText;

        // ── Runtime state ────────────────────────────────────────────────────────

        private InteractableItem _heldItem;
        private Rigidbody        _heldRb;
        private Collider[]       _heldColliders;
        private string           _hoverLabel;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>The item currently in the player's hand (null if empty).</summary>
        public InteractableItem HeldItem => _heldItem;

        /// <summary>True when the player is not holding anything.</summary>
        public bool IsHandEmpty => _heldItem == null;

        /// <summary>Set by stations (e.g. during appliance processing) to block all input.</summary>
        public bool InputBlocked { get; set; }

        // ── Unity loop ───────────────────────────────────────────────────────────

        private void Update()
        {
            if (InputBlocked) return;
            HandleInput();
            UpdateHover();
        }

        // ── Input ────────────────────────────────────────────────────────────────

        private void HandleInput()
        {
            // Interactions are only processed while the cursor is locked
            // (i.e. the game is in focus). PlayerLook manages lock/unlock.
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            // Right click → drop/throw held item
            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_heldItem != null)
                    Drop();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;

            GameObject hit = RaycastTarget();
            if (hit == null) return;

            // ── Try IAssemblyInteractable (PrepStation, appliances, etc.) ────────
            IAssemblyInteractable interactable = hit.GetComponent<IAssemblyInteractable>()
                                              ?? hit.GetComponentInParent<IAssemblyInteractable>();
            if (interactable != null && interactable.CanInteract(this))
            {
                interactable.Interact(this);
                return;
            }

            // ── Empty hand + InteractableItem → pick it up ──────────────────────
            if (_heldItem == null)
            {
                InteractableItem item = hit.GetComponent<InteractableItem>()
                                     ?? hit.GetComponentInParent<InteractableItem>();
                if (item != null)
                    PickUp(item);
            }
        }

        // ── Hover detection ──────────────────────────────────────────────────────

        /// <summary>
        /// Runs every frame. When the hand is empty, raycasts from the screen
        /// centre and resolves a display label for whatever interactable the
        /// crosshair is pointing at, then refreshes the status text.
        /// </summary>
        private void UpdateHover()
        {
            // Hover only makes sense when the hand is empty and game is in focus
            if (_heldItem != null || Cursor.lockState != CursorLockMode.Locked)
            {
                if (_hoverLabel != null)
                {
                    _hoverLabel = null;
                    RefreshUI();
                }
                return;
            }

            GameObject hit   = RaycastTarget();
            string     label = hit != null ? GetHoverLabel(hit) : null;

            if (label == _hoverLabel) return; // no change — skip RefreshUI call

            _hoverLabel = label;
            RefreshUI();
        }

        /// <summary>
        /// Resolves a human-readable label for the object under the crosshair.
        /// Priority: InteractableItem.itemName → IngredientStorage.storageName
        ///           → any IAssemblyInteractable (uses GameObject name).
        /// Returns null if the object has no relevant component.
        /// </summary>
        private string GetHoverLabel(GameObject hit)
        {
            // Pickable world item
            InteractableItem item = hit.GetComponent<InteractableItem>()
                                  ?? hit.GetComponentInParent<InteractableItem>();
            if (item != null) return item.itemName;

            // Infinite ingredient source
            IngredientStorage storage = hit.GetComponent<IngredientStorage>()
                                      ?? hit.GetComponentInParent<IngredientStorage>();
            if (storage != null && storage.data != null) return storage.data.storageName;

            // Any other station / appliance (PrepStation, Grill, Toaster, …)
            IAssemblyInteractable interactable = hit.GetComponent<IAssemblyInteractable>()
                                               ?? hit.GetComponentInParent<IAssemblyInteractable>();
            if (interactable != null) return hit.name;

            return null;
        }

        // ── Hand operations ──────────────────────────────────────────────────────

        /// <summary>
        /// Pick up an InteractableItem from the world.
        /// Disables physics/colliders and parents the object to the hold point.
        /// </summary>
        public void PickUp(InteractableItem item)
        {
            if (_heldItem != null) return;

            _heldItem = item;

            // Disable colliders FIRST — eliminates any chance of the item's collider
            // interacting with the CharacterController between now and kinematic toggle.
            _heldColliders = item.GetComponentsInChildren<Collider>();
            foreach (Collider col in _heldColliders)
                col.enabled = false;

            // Handle Rigidbody.
            // Only zero velocity when the body is still dynamic — setting velocity on
            // a kinematic body is unsupported and logs a warning (e.g. when collecting
            // from the Grill, where the body was already kinematic on the slot).
            _heldRb = item.GetComponent<Rigidbody>();
            if (_heldRb != null)
            {
                if (!_heldRb.isKinematic)
                {
                    _heldRb.linearVelocity  = Vector3.zero;
                    _heldRb.angularVelocity = Vector3.zero;
                }
                _heldRb.isKinematic = true;
            }

            // Parent to the hold point — the item's own mesh is the visual
            item.transform.SetParent(holdPoint);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            item.OnPickedUp();
            RefreshUI();
        }

        /// <summary>
        /// Drop/throw the held item forward from the camera.
        /// Re-enables physics and colliders.
        /// </summary>
        public void Drop()
        {
            if (_heldItem == null) return;

            InteractableItem item = _heldItem;
            _heldItem = null;

            // Unparent from hold point
            item.transform.SetParent(null);

            // Re-enable colliders
            if (_heldColliders != null)
            {
                foreach (Collider col in _heldColliders)
                    if (col != null) col.enabled = true;
            }

            // Position slightly in front of the camera and toss forward
            Camera cam = Camera.main;
            if (cam != null)
            {
                item.transform.position = cam.transform.position + cam.transform.forward * 1.5f;

                if (_heldRb != null)
                {
                    _heldRb.isKinematic = false;
                    _heldRb.linearVelocity = cam.transform.forward * throwForce;
                }
            }

            item.OnDropped();

            _heldRb = null;
            _heldColliders = null;
            RefreshUI();
        }

        /// <summary>
        /// Releases the held item from the hand without repositioning or physics.
        /// The item stays in its dormant state (kinematic, colliders disabled).
        /// The caller (e.g. PrepStation) is responsible for positioning it.
        /// Returns the released item.
        /// </summary>
        public InteractableItem Release()
        {
            if (_heldItem == null) return null;

            InteractableItem item = _heldItem;
            _heldItem = null;

            // Unparent — item keeps kinematic + disabled colliders from PickUp
            item.transform.SetParent(null);

            _heldRb = null;
            _heldColliders = null;
            RefreshUI();

            return item;
        }

        /// <summary>
        /// Silently removes the held item (destroys the GameObject).
        /// Used when delivering an order or consuming the item.
        /// </summary>
        public void DestroyHeldItem()
        {
            if (_heldItem == null) return;

            Destroy(_heldItem.gameObject);
            _heldItem = null;
            _heldRb = null;
            _heldColliders = null;
            RefreshUI();
        }

        // ── UI ────────────────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            if (handStatusText == null) return;

            if (_heldItem != null)
                handStatusText.text = _heldItem.itemName;       // holding something
            else if (_hoverLabel != null)
                handStatusText.text = _hoverLabel;              // hovering an interactable
            else
                handStatusText.text = string.Empty;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private GameObject RaycastTarget()
        {
            Camera cam = Camera.main;
            if (cam == null) return null;

            // Always shoot from the viewport centre — this aligns with the crosshair
            // and works correctly regardless of cursor lock state.
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return Physics.Raycast(ray, out RaycastHit hit, interactRange)
                ? hit.collider.gameObject
                : null;
        }
    }
}
