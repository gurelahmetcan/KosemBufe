using UnityEngine;
using TMPro;

/// <summary>
/// Item-in-hand controller.
///
/// Mechanic:
///  - Left-click a HoldableItem source  → copy into hand (source stays in scene).
///  - Right-click anywhere              → drop held item; it disappears.
///  - Only 1 item can be held at a time.
///  - Left-click the CuttingBoard with a SliceableItem in hand → instant cut.
///  - Bottom-screen text shows "Full <name>" or "Sliced <name>".
///
/// Scene Setup:
///  1. Attach this component to any persistent GameObject (e.g. "GameController").
///  2. handDisplayPoint – Empty child of Main Camera at the bottom-right of the viewport
///                        (e.g. local pos 0.35, -0.30, 0.65). Held prefab parents here.
///  3. handStatusText   – TextMeshProUGUI on a Screen Space Overlay Canvas,
///                        anchored to the bottom of the screen.
///  4. Source objects   – Add HoldableItem (+ Collider) to every pickupable item.
///                        Also add SliceableItem if the item can be cut on the board.
///  5. Cutting board    – Add CuttingBoard (+ Collider) to the board object.
/// </summary>
public class SlicingController : MonoBehaviour
{
    [Header("Hand Display")]
    [Tooltip("Empty child of Main Camera placed at the bottom-right of the view. " +
             "The held item prefab is parented here.")]
    public Transform handDisplayPoint;

    [Header("Input")]
    [Tooltip("Maximum raycast distance for click detection.")]
    public float clickRange = 10f;

    [Header("UI")]
    [Tooltip("TextMeshProUGUI element anchored at the bottom of the screen.")]
    public TextMeshProUGUI handStatusText;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private HoldableItem _heldItem;       // source component of the item currently in hand
    private bool         _heldIsSliced;   // true after cutting on the board
    private GameObject   _heldVisual;     // instantiated prefab parented to handDisplayPoint

    // ── Unity loop ────────────────────────────────────────────────────────────

    private void Update()
    {
        HandleInput();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        // Right-click → drop whatever is in hand
        if (Input.GetMouseButtonDown(1))
        {
            if (_heldItem != null)
                DropItem();
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;

        GameObject hit = RaycastTarget();
        if (hit == null) return;

        // Holding an unsliced SliceableItem + clicked the cutting board → cut it
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

        // Empty hand + clicked a HoldableItem source → pick it up
        if (_heldItem == null)
        {
            HoldableItem holdable = hit.GetComponent<HoldableItem>()
                                 ?? hit.GetComponentInParent<HoldableItem>();
            if (holdable != null)
                PickUp(holdable);
        }
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
            handStatusText.text = string.Empty;
        else if (_heldIsSliced)
            handStatusText.text = $"Sliced {_heldItem.itemName}";
        else
            handStatusText.text = $"Full {_heldItem.itemName}";
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
