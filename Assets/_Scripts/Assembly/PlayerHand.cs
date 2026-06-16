using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

        [Tooltip("Forward force applied to the item when thrown (right-click held).")]
        public float throwForce = 5f;

        [Tooltip("How long the right mouse button must be held before it counts as a throw instead of a drop.")]
        public float throwHoldThreshold = 0.25f;

        [Header("UI")]
        [Tooltip("Optional TextMeshProUGUI showing what the player is currently holding.")]
        public TextMeshProUGUI handStatusText;

        // ── Runtime state ────────────────────────────────────────────────────────

        private InteractableItem _heldItem;
        private Rigidbody        _heldRb;
        private Collider[]       _heldColliders;
        private string           _hoverLabel;
        private float            _rightPressTime = -1f;

        // ── Public API ───────────────────────────────────────────────────────────

        public InteractableItem HeldItem    => _heldItem;
        public bool             IsHandEmpty => _heldItem == null;

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
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                _rightPressTime = Time.time;
            }
            else if (mouse.rightButton.wasReleasedThisFrame)
            {
                if (_heldItem != null)
                {
                    bool throwIt = _rightPressTime >= 0f && Time.time - _rightPressTime >= throwHoldThreshold;
                    Drop(throwIt);
                }
                _rightPressTime = -1f;
            }

            if (mouse.rightButton.isPressed) return;

            if (!mouse.leftButton.wasPressedThisFrame) return;

            GameObject hit = RaycastTarget();
            if (hit == null) return;

            IAssemblyInteractable interactable = hit.GetComponent<IAssemblyInteractable>()
                                              ?? hit.GetComponentInParent<IAssemblyInteractable>();
            if (interactable != null && interactable.CanInteract(this))
            {
                interactable.Interact(this);
                return;
            }

            if (_heldItem == null)
            {
                InteractableItem item = hit.GetComponent<InteractableItem>()
                                     ?? hit.GetComponentInParent<InteractableItem>();
                if (item != null)
                    PickUp(item);
            }
        }

        // ── Hover detection ──────────────────────────────────────────────────────

        private void UpdateHover()
        {
            if (_heldItem != null || Cursor.lockState != CursorLockMode.Locked)
            {
                ClearHover();
                return;
            }

            GameObject hit = RaycastTarget();

            // Status text label
            string label = hit != null ? GetHoverLabel(hit) : null;
            if (label == _hoverLabel) return;
            _hoverLabel = label;
            RefreshUI();
        }

        private void ClearHover()
        {
            if (_hoverLabel == null) return;
            _hoverLabel = null;
            RefreshUI();
        }

        private string GetHoverLabel(GameObject hit)
        {
            InteractableItem item = hit.GetComponent<InteractableItem>()
                                  ?? hit.GetComponentInParent<InteractableItem>();
            if (item != null) return item.itemName;

            IngredientStorage storage = hit.GetComponent<IngredientStorage>()
                                      ?? hit.GetComponentInParent<IngredientStorage>();
            if (storage != null && storage.data != null) return storage.data.storageName;

            IAssemblyInteractable interactable = hit.GetComponent<IAssemblyInteractable>()
                                               ?? hit.GetComponentInParent<IAssemblyInteractable>();
            if (interactable != null) return hit.name;

            return null;
        }

        // ── Hand operations ──────────────────────────────────────────────────────

        public void PickUp(InteractableItem item)
        {
            if (_heldItem != null) return;

            _heldItem = item;

            _heldColliders = item.GetComponentsInChildren<Collider>();
            foreach (Collider col in _heldColliders)
                col.enabled = false;

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

            item.transform.SetParent(holdPoint);
            item.transform.localPosition = item.InHandLocalPos;
            item.transform.localRotation = item.InHandLocalRotation;

            item.OnPickedUp();
            RefreshUI();
        }

        public void Drop(bool throwIt = true)
        {
            if (_heldItem == null) return;

            InteractableItem item = _heldItem;
            _heldItem = null;

            item.transform.SetParent(null);

            if (_heldColliders != null)
                foreach (Collider col in _heldColliders)
                    if (col != null) col.enabled = true;

            Camera cam = Camera.main;
            if (_heldRb != null)
            {
                _heldRb.isKinematic    = false;
                _heldRb.linearVelocity = (throwIt && cam != null) ? cam.transform.forward * throwForce : Vector3.zero;
            }

            item.OnDropped();

            _heldRb        = null;
            _heldColliders = null;
            RefreshUI();
        }

        /// <summary>
        /// Releases the held item without repositioning or applying physics.
        /// The caller is responsible for positioning and managing the item.
        /// Returns the released item.
        /// </summary>
        public InteractableItem Release()
        {
            if (_heldItem == null) return null;

            InteractableItem item = _heldItem;
            _heldItem = null;

            item.transform.SetParent(null);

            _heldRb        = null;
            _heldColliders = null;
            RefreshUI();

            return item;
        }

        /// <summary>Destroys the held item. Used when delivering an order.</summary>
        public void DestroyHeldItem()
        {
            if (_heldItem == null) return;

            Destroy(_heldItem.gameObject);
            _heldItem      = null;
            _heldRb        = null;
            _heldColliders = null;
            RefreshUI();
        }

        // ── UI ────────────────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            if (handStatusText == null) return;

            if (_heldItem != null)
                handStatusText.text = _heldItem.itemName;
            else if (_hoverLabel != null)
                handStatusText.text = _hoverLabel;
            else
                handStatusText.text = string.Empty;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private GameObject RaycastTarget()
        {
            Camera cam = Camera.main;
            if (cam == null) return null;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return Physics.Raycast(ray, out RaycastHit hit, interactRange)
                ? hit.collider.gameObject
                : null;
        }
    }
}
