using Bufe;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


/// <summary>
/// Item-in-hand controller. Handles pick-up, drop, and visual display.
/// Interaction logic lives on the interactable objects themselves (IInteractable).
/// </summary>
public class InHandController : MonoBehaviour
{
    [Header("Hand Display")]
    [Tooltip("Empty child of Main Camera placed at the bottom-right of the view.")]
    public Transform handDisplayPoint;

    [Header("Input")]
    [Tooltip("Maximum raycast distance for click detection.")]
    public float clickRange = 10f;

    [Header("UI")]
    [Tooltip("TextMeshProUGUI showing what is currently in hand.")]
    public TextMeshProUGUI handStatusText;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private HoldableItem _heldItem;        // source component of the item in hand
    private bool         _heldIsSliced;    // true after cutting on the board
    private GameObject   _heldVisual;      // prefab instance parented to handDisplayPoint
    private HolderItem   _hoveredHolder;   // holder whose label is currently shown

    // ── Public API (used by stations and external systems) ────────────────────

    /// <summary>The item currently in the player's hand (null if empty).</summary>
    public HoldableItem HeldItem => _heldItem;

    /// <summary>Whether the held item has been sliced.</summary>
    public bool IsSliced => _heldIsSliced;

    /// <summary>Set by stations (e.g. during minigames) to block all player input.</summary>
    public bool InputBlocked { get; set; }

    // ── Unity loop ────────────────────────────────────────────────────────────

    private void Update()
    {
        HandleInput();
        UpdateHoverUI();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (InputBlocked) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Right-click → drop (clear holder contents first if applicable)
        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (_heldItem != null)
            {
                HolderItem heldHolder = _heldItem.GetComponent<HolderItem>();
                if (heldHolder != null)
                    heldHolder.ClearItems();

                DropItem();
            }
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame) return;

        GameObject hit = RaycastTarget();
        if (hit == null) return;

        // ── Try IInteractable (stations, holders, etc.) ─────────────────────
        IInteractable interactable = hit.GetComponent<IInteractable>()
                                  ?? hit.GetComponentInParent<IInteractable>();
        if (interactable != null && interactable.CanInteract(this))
        {
            interactable.Interact(this);
            return;
        }

        // ── Empty hand + any HoldableItem → pick it up ─────────────────────
        if (_heldItem == null)
        {
            HoldableItem holdable = hit.GetComponent<HoldableItem>()
                                 ?? hit.GetComponentInParent<HoldableItem>();
            if (holdable != null)
                PickUp(holdable);
        }
    }

    // ── Hover UI ──────────────────────────────────────────────────────────────

    private void UpdateHoverUI()
    {
        if (InputBlocked) return;

        GameObject hit = RaycastTarget();

        HolderItem newHolder = null;
        if (hit != null)
        {
            newHolder = hit.GetComponent<HolderItem>()
                     ?? hit.GetComponentInParent<HolderItem>();
        }

        if (newHolder == _hoveredHolder) return; // nothing changed

        if (_hoveredHolder != null)
            _hoveredHolder.HideLabel();

        _hoveredHolder = newHolder;

        if (_hoveredHolder != null)
            _hoveredHolder.ShowLabel();
    }

    // ── Hand operations ───────────────────────────────────────────────────────

    public void PickUp(HoldableItem item)
    {
        Debug.Log($"Picking up {item.itemName}, {item.cookState}, {item.preparedIngredients}");
        _heldItem     = item;
        _heldIsSliced = false;
        RefreshVisual();
        RefreshUI();
    }

    /// <summary>Marks the held item as sliced and swaps the visual.</summary>
    public void SliceHeldItem()
    {
        _heldIsSliced = true;
        RefreshVisual();
        RefreshUI();
    }

    /// <summary>Drops the held item (clears hand state and visual).</summary>
    public void DropItem()
    {
        _heldItem     = null;
        _heldIsSliced = false;
        ClearVisual();
        RefreshUI();
    }

    /// <summary>
    /// Destroys the held item GameObject and clears the hand.
    /// Called by OrderManager when an order is delivered.
    /// </summary>
    public void DestroyHeldItem()
    {
        if (_heldItem != null)
        {
            HolderItem holder = _heldItem.GetComponent<HolderItem>();
            if (holder != null)
            {
                // Scene holder (e.g. Bread): reset to empty state instead of destroying
                holder.ClearItems();
                _heldItem.preparedIngredients.Clear();
                _heldItem.cookState = Constants.CookState.Raw;
            }
            else
            {
                Destroy(_heldItem.gameObject);
            }
        }

        DropItem();
    }

    /// <summary>Clears the currently hovered holder label (used by stations before minigames).</summary>
    public void ClearHoverLabel()
    {
        if (_hoveredHolder != null)
        {
            _hoveredHolder.HideLabel();
            _hoveredHolder = null;
        }
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    private void RefreshVisual()
    {
        ClearVisual();

        if (_heldItem == null || handDisplayPoint == null) return;

        GameObject prefab;
        if (_heldIsSliced)
        {
            SliceableItem sliceable = _heldItem.GetComponent<SliceableItem>();
            prefab = sliceable != null ? sliceable.slicedHandPrefab : null;
        }
        else
        {
            prefab = _heldItem.handPrefab;
        }

        if (prefab == null) return;

        _heldVisual = Instantiate(prefab, handDisplayPoint);
        _heldVisual.transform.localPosition = Vector3.zero;
        _heldVisual.transform.localRotation = Quaternion.identity;
    }

    private void ClearVisual()
    {
        if (_heldVisual != null)
        {
            Destroy(_heldVisual);
            _heldVisual = null;
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (handStatusText == null) return;

        if (_heldItem == null)
        {
            handStatusText.text = string.Empty;
            return;
        }

        HolderItem holder = _heldItem.GetComponent<HolderItem>();
        if (holder != null)
        {
            handStatusText.text = $"{_heldItem.itemName} ({holder.StoredCount}/{holder.maxSlots})";
            return;
        }

        handStatusText.text = _heldIsSliced
            ? $"Sliced {_heldItem.itemName}"
            : $"Full {_heldItem.itemName}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private GameObject RaycastTarget()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return null;

        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        return Physics.Raycast(ray, out RaycastHit hit, clickRange)
            ? hit.collider.gameObject
            : null;
    }
}
