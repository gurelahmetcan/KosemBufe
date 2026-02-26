using UnityEngine;
using TMPro;

/// <summary>
/// Item-in-hand controller.
///
/// Mechanic:
///  - Left-click a SliceableSource  → item copied into hand (source stays in scene).
///  - Right-click anywhere          → drop item; it disappears.
///  - Only 1 item can be held at a time.
///  - Left-click the CuttingBoard with an unsliced item in hand → item instantly becomes sliced.
///  - A text label at the bottom of the screen shows what is currently held.
///
/// Scene Setup:
///  1. Attach this component to any persistent GameObject (e.g. "GameController").
///  2. handDisplayPoint  – Create an empty child of Main Camera, position it at
///                         the bottom-right of the viewport (e.g. local pos 0.35, -0.30, 0.65).
///                         The held item's prefab will be parented here.
///  3. handStatusText    – A TextMeshProUGUI on a Screen Space – Overlay Canvas,
///                         anchored to the bottom-centre of the screen.
///  4. Cutting board     – Add a CuttingBoard component (and a Collider) to the board object.
///  5. Source objects    – Add a SliceableSource component (and a Collider) to every
///                         grabbable item; assign its SliceableItemData asset.
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

    private SliceableItemData _heldData;      // data of the item currently in hand (null = empty)
    private bool              _heldIsSliced;  // true after the item has been cut on the board
    private GameObject        _heldVisual;    // the instantiated 3D prefab parented to handDisplayPoint

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
            if (_heldData != null)
                DropItem();
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;

        GameObject hit = RaycastTarget();
        if (hit == null) return;

        // Holding an unsliced item + clicked the cutting board → cut it
        if (_heldData != null && !_heldIsSliced)
        {
            CuttingBoard board = hit.GetComponent<CuttingBoard>()
                              ?? hit.GetComponentInParent<CuttingBoard>();
            if (board != null)
            {
                CutHeldItem();
                return;
            }
        }

        // Empty hand + clicked a sliceable source → pick it up
        if (_heldData == null)
        {
            SliceableSource source = hit.GetComponent<SliceableSource>()
                                  ?? hit.GetComponentInParent<SliceableSource>();
            if (source != null && source.itemData != null)
                PickUp(source.itemData);
        }
    }

    // ── Hand operations ───────────────────────────────────────────────────────

    private void PickUp(SliceableItemData data)
    {
        _heldData     = data;
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
        _heldData     = null;
        _heldIsSliced = false;
        ClearVisual();
        RefreshUI();
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    private void RefreshVisual()
    {
        ClearVisual();

        if (_heldData == null || handDisplayPoint == null) return;

        GameObject prefab = _heldIsSliced ? _heldData.slicePrefab : _heldData.boardPrefab;
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

        if (_heldData == null)
            handStatusText.text = string.Empty;
        else if (_heldIsSliced)
            handStatusText.text = $"Sliced {_heldData.itemName}";
        else
            handStatusText.text = $"Full {_heldData.itemName}";
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
