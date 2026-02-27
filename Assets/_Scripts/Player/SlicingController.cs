using UnityEngine;
using TMPro;

/// <summary>
/// Item-in-hand controller. Handles pick-up, drop, slicing, and holder interactions.
///
/// Full interaction matrix:
///  LEFT-CLICK
///   • Empty hand  + SliceableSource / HoldableItem  → pick it up.
///   • Unsliced    + CuttingBoard (item has SliceableItem) → instant cut.
///   • Sliced item + HolderItem (not full)            → store in holder, clear hand.
///   • Empty hand  + HolderItem                       → pick up holder.
///   • Loaded HolderItem in hand + ToasterStation     → start toaster minigame.
///
///  RIGHT-CLICK
///   • Holding a HolderItem  → clear its stored items, then drop.
///   • Holding anything else → drop (disappears).
///
///  HOVER
///   • Mouse over HolderItem → that holder's own child label is shown: "{name}: X/Y".
///   (Input and hover are fully blocked while the toaster minigame is active.)
///
/// Scene Setup:
///  1. Attach to any persistent GameObject (e.g. "GameController").
///  2. handDisplayPoint – Empty child of Main Camera, bottom-right of viewport
///                        (e.g. local pos 0.35, -0.30, 0.65).
///  3. handStatusText   – TextMeshProUGUI anchored bottom-centre of screen.
///  4. Each holder prefab – add a child TextMeshPro label, position it freely,
///                          start it disabled, assign it to HolderItem.hoverLabel.
///  5. Source objects   – HoldableItem + Collider (+ SliceableItem if cuttable,
///                        + HolderItem if it stores other items).
///  6. Cutting board    – CuttingBoard + Collider.
/// </summary>
public class SlicingController : MonoBehaviour
{
    [Header("Hand Display")]
    [Tooltip("Empty child of Main Camera placed at the bottom-right of the view.")]
    public Transform handDisplayPoint;

    [Header("Input")]
    [Tooltip("Maximum raycast distance for click detection.")]
    public float clickRange = 10f;

    [Header("Minigame")]
    [Tooltip("The ToasterMinigame controller in the scene.")]
    public ToasterMinigame toasterMinigame;

    [Header("UI")]
    [Tooltip("TextMeshProUGUI showing what is currently in hand.")]
    public TextMeshProUGUI handStatusText;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private HoldableItem _heldItem;        // source component of the item in hand
    private bool         _heldIsSliced;    // true after cutting on the board
    private GameObject   _heldVisual;      // prefab instance parented to handDisplayPoint
    private HolderItem   _hoveredHolder;   // holder whose label is currently shown

    // ── Unity loop ────────────────────────────────────────────────────────────

    private void Update()
    {
        HandleInput();
        UpdateHoverUI();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (toasterMinigame != null && toasterMinigame.IsActive) return;
        // Right-click → drop (clear holder contents first if applicable)
        if (Input.GetMouseButtonDown(1))
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

        if (!Input.GetMouseButtonDown(0)) return;

        GameObject hit = RaycastTarget();
        if (hit == null) return;

        // ── Loaded HolderItem in hand + toaster → start minigame ─────────────
        if (_heldItem != null && toasterMinigame != null)
        {
            HolderItem heldHolder = _heldItem.GetComponent<HolderItem>();
            if (heldHolder != null && heldHolder.StoredCount > 0)
            {
                ToasterStation toaster = hit.GetComponent<ToasterStation>()
                                      ?? hit.GetComponentInParent<ToasterStation>();
                if (toaster != null)
                {
                    StartToasterMinigame(toaster);
                    return;
                }
            }
        }

        // ── Unsliced item in hand + cutting board → cut ───────────────────────
        if (_heldItem != null && !_heldIsSliced)
        {
            CuttingBoard board = hit.GetComponent<CuttingBoard>()
                              ?? hit.GetComponentInParent<CuttingBoard>();
            if (board != null && _heldItem.GetComponent<SliceableItem>() != null)
            {
                CutHeldItem();
                return;
            }
        }

        // ── Sliced item in hand + holder → store in holder ────────────────────
        if (_heldItem != null && _heldIsSliced)
        {
            HolderItem holder = hit.GetComponent<HolderItem>()
                             ?? hit.GetComponentInParent<HolderItem>();
            if (holder != null && !holder.IsFull)
            {
                holder.AddItem(_heldItem);
                DropItem();
                return;
            }
        }

        // ── Empty hand + any HoldableItem (including holders) → pick it up ────
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
        if (toasterMinigame != null && toasterMinigame.IsActive) return;
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

    // ── Toaster minigame ──────────────────────────────────────────────────────

    private void StartToasterMinigame(ToasterStation toaster)
    {
        // Hide any open hover label and clear hand — bread goes into the toaster
        if (_hoveredHolder != null)
        {
            _hoveredHolder.HideLabel();
            _hoveredHolder = null;
        }
        DropItem();

        toasterMinigame.StartMinigame(success =>
        {
            HoldableItem result = success ? toaster.successItem : toaster.failureItem;
            if (result != null)
                PickUp(result);
        });
    }

    // ── Hand operations ───────────────────────────────────────────────────────

    private void PickUp(HoldableItem item)
    {
        _heldItem     = item;
        _heldIsSliced = false;
        RefreshVisual();
        RefreshUI();
    }

    private void CutHeldItem()
    {
        _heldIsSliced = true;
        RefreshVisual();
        RefreshUI();
    }

    private void DropItem()
    {
        _heldItem     = null;
        _heldIsSliced = false;
        ClearVisual();
        RefreshUI();
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, clickRange)
            ? hit.collider.gameObject
            : null;
    }
}
