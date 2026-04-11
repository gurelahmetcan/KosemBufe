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
    }

    // ── Input ────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Right click → drop/throw held item
        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (_heldItem != null)
                Drop();
            return;
        }

        // F key → flip whatever IFlippable is under the cursor (e.g. grill items)
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame)
        {
            GameObject flipHit = RaycastTarget();
            if (flipHit != null)
            {
                IFlippable flippable = flipHit.GetComponent<IFlippable>()
                                    ?? flipHit.GetComponentInParent<IFlippable>();
                if (flippable != null && flippable.CanFlip())
                    flippable.Flip();
            }
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

    // ── Hand operations ──────────────────────────────────────────────────────

    /// <summary>
    /// Pick up an InteractableItem from the world.
    /// Disables physics/colliders and parents the object to the hold point.
    /// </summary>
    public void PickUp(InteractableItem item)
    {
        if (_heldItem != null) return;

        _heldItem = item;

        // Cache and disable Rigidbody
        _heldRb = item.GetComponent<Rigidbody>();
        if (_heldRb != null)
        {
            _heldRb.linearVelocity = Vector3.zero;
            _heldRb.angularVelocity = Vector3.zero;
            _heldRb.isKinematic = true;
        }

        // Cache and disable all colliders so the held item doesn't block raycasts
        _heldColliders = item.GetComponentsInChildren<Collider>();
        foreach (Collider col in _heldColliders)
            col.enabled = false;

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
        handStatusText.text = _heldItem != null ? _heldItem.itemName : string.Empty;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private GameObject RaycastTarget()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return null;

        Camera cam = Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        return Physics.Raycast(ray, out RaycastHit hit, interactRange)
            ? hit.collider.gameObject
            : null;
    }
}
