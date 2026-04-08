using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for the deep fryer panel.
///
/// Scene Setup:
///   1. Create a Screen Space – Overlay Canvas. Start it disabled.
///   2. Inside it build the layout (see screenshot reference):
///        trayArea        — left RectTransform, holds visual stock slices.
///        vatArea         — center RectTransform (black background), fry slices appear here.
///        collectionArea  — bottom-center RectTransform (gray circle), collected fries land here.
///   3. Right-side buttons: Ketchup, Mayo, Go Back.
///   4. Center buttons (shown when idle): Small, Large.
///   5. Additional: Clear, Finish buttons.
///   6. Assign slicePrefab: a small rectangular UI Image with FrySliceUI component.
///   7. Assign cameraRotation to pause the camera during the UI.
///   8. Assign stockText and statusText TMP labels.
/// </summary>
public class FryerUI : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Player camera rotation — disabled while the fryer UI is open.")]
    public PlayerCameraRotation cameraRotation;

    [Header("Panel")]
    [Tooltip("Root panel GameObject (the Canvas or a child panel). Toggled on/off.")]
    public GameObject panel;

    [Header("Areas")]
    [Tooltip("Left-side RectTransform where raw stock slice visuals are shown.")]
    public RectTransform trayArea;

    [Tooltip("Center RectTransform (black background) where frying slices appear.")]
    public RectTransform vatArea;

    [Tooltip("Bottom-center RectTransform (gray circle) where collected fries land.")]
    public RectTransform collectionArea;

    [Header("Batch Buttons")]
    [Tooltip("Start a small batch (6 slices).")]
    public Button smallButton;

    [Tooltip("Start a large batch (12 slices).")]
    public Button largeButton;

    [Header("Action Buttons")]
    [Tooltip("Apply ketchup to the collected fries.")]
    public Button ketchupButton;

    [Tooltip("Apply mayonnaise to the collected fries.")]
    public Button mayoButton;

    [Tooltip("Finish without sauce (plain fries).")]
    public Button plainButton;

    [Tooltip("Clear remaining (burnt) slices from the vat.")]
    public Button clearButton;

    [Tooltip("Finish the order — puts the fries into the player's hand.")]
    public Button finishButton;

    [Tooltip("Close the fryer UI and return to gameplay.")]
    public Button goBackButton;

    [Header("Text")]
    [Tooltip("Displays current raw-slice stock count.")]
    public TMP_Text stockText;

    [Tooltip("Displays batch status (Cooking…, Ready!, Burnt!, etc.).")]
    public TMP_Text statusText;

    [Header("Slice Visuals")]
    [Tooltip("UI prefab: small rectangular Image + FrySliceUI component.")]
    public FrySliceUI slicePrefab;

    [Tooltip("Max number of slice images shown in the tray.")]
    public int maxTraySlices = 8;

    [Tooltip("Vertical offset between stacked tray slices (pixels).")]
    public float trayStackOffset = 12f;

    // ── Slice colors ─────────────────────────────────────────────────────────

    private static readonly Color RawColor     = new Color(0.95f, 0.92f, 0.70f);
    private static readonly Color CookingColor = new Color(0.95f, 0.92f, 0.70f);
    private static readonly Color ReadyColor   = new Color(1.00f, 0.85f, 0.20f);
    private static readonly Color BurntColor   = new Color(0.55f, 0.35f, 0.15f);

    // ── Runtime ──────────────────────────────────────────────────────────────

    private FryerStation      _station;
    private InHandController  _hand;

    private readonly List<FrySliceUI>  _vatSlices        = new List<FrySliceUI>();
    private readonly List<FrySliceUI>  _collectedSlices  = new List<FrySliceUI>();
    private readonly List<GameObject>  _trayVisuals      = new List<GameObject>();

    public bool IsOpen { get; private set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (smallButton   != null) smallButton.onClick.AddListener(OnSmallClicked);
        if (largeButton   != null) largeButton.onClick.AddListener(OnLargeClicked);
        if (ketchupButton != null) ketchupButton.onClick.AddListener(() => OnSauceClicked(IngredientType.Ketchup));
        if (mayoButton    != null) mayoButton.onClick.AddListener(() => OnSauceClicked(IngredientType.Mayonnaise));
        if (plainButton   != null) plainButton.onClick.AddListener(OnPlainClicked);
        if (clearButton   != null) clearButton.onClick.AddListener(OnClearClicked);
        if (finishButton  != null) finishButton.onClick.AddListener(OnFinishClicked);
        if (goBackButton  != null) goBackButton.onClick.AddListener(OnGoBackClicked);
    }

    private void Update()
    {
        if (!IsOpen) return;

        RefreshVatColors();
        RefreshButtons();
        RefreshStatus();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Open(FryerStation station, InHandController hand)
    {
        _station = station;
        _hand    = hand;
        IsOpen   = true;

        if (cameraRotation != null) cameraRotation.enabled = false;

        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        IsOpen = false;
        panel.SetActive(false);

        if (cameraRotation != null) cameraRotation.enabled = true;
        if (_hand != null) _hand.InputBlocked = false;

        ClearAllVisuals();
    }

    /// <summary>Full rebuild of all visuals (called on Open and after stock changes).</summary>
    public void Refresh()
    {
        RefreshStockText();
        RefreshTrayVisuals();
        RebuildVatSlices();
        RebuildCollectedSlices();
        RefreshButtons();
        RefreshStatus();
    }

    /// <summary>Called by FrySliceUI when a vat slice is clicked.</summary>
    public void OnSliceClicked(FrySliceUI slice)
    {
        if (_station == null || _station.CurrentBatch == null) return;
        if (_station.CurrentBatch.GetState() != FryState.Ready) return;

        if (_station.TryCollectSlice())
        {
            _vatSlices.Remove(slice);
            MoveSliceToCollection(slice);
            RefreshButtons();
            RefreshStatus();
        }
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    private void OnSmallClicked()
    {
        if (_station != null && _station.CanStartBatch(FryerBatch.Size.Small))
        {
            _station.StartBatch(FryerBatch.Size.Small);
            Refresh();
        }
    }

    private void OnLargeClicked()
    {
        if (_station != null && _station.CanStartBatch(FryerBatch.Size.Large))
        {
            _station.StartBatch(FryerBatch.Size.Large);
            Refresh();
        }
    }

    private void OnSauceClicked(IngredientType sauce)
    {
        if (_station != null)
        {
            _station.SelectSauce(sauce);
            RefreshButtons();
        }
    }

    private void OnPlainClicked()
    {
        if (_station != null)
        {
            _station.SelectSauce(IngredientType.None);
            RefreshButtons();
        }
    }

    private void OnClearClicked()
    {
        if (_station == null) return;

        _station.ClearVat();

        foreach (FrySliceUI s in _vatSlices)
            if (s != null) Destroy(s.gameObject);
        _vatSlices.Clear();

        RefreshButtons();
        RefreshStatus();
    }

    private void OnFinishClicked()
    {
        if (_station != null && _station.Finish(_hand))
            Close();
    }

    private void OnGoBackClicked()
    {
        Close();
    }

    // ── Visual builders ──────────────────────────────────────────────────────

    private void RefreshStockText()
    {
        if (stockText != null)
            stockText.text = $"Stock: {_station.RawSliceStock}";
    }

    private void RefreshTrayVisuals()
    {
        foreach (GameObject g in _trayVisuals)
            Destroy(g);
        _trayVisuals.Clear();

        if (slicePrefab == null || trayArea == null) return;

        int display = Mathf.Min(_station.RawSliceStock, maxTraySlices);
        for (int i = 0; i < display; i++)
        {
            FrySliceUI slice = Instantiate(slicePrefab, trayArea);
            slice.transform.localPosition = new Vector3(0f, i * trayStackOffset, 0f);
            slice.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));
            slice.SetColor(RawColor);
            slice.enabled = false; // not interactive
            _trayVisuals.Add(slice.gameObject);
        }
    }

    private void RebuildVatSlices()
    {
        foreach (FrySliceUI s in _vatSlices)
            if (s != null) Destroy(s.gameObject);
        _vatSlices.Clear();

        if (_station.CurrentBatch == null) return;
        if (slicePrefab == null || vatArea == null) return;

        int remaining = _station.CurrentBatch.RemainingInVat;
        Rect r = vatArea.rect;

        for (int i = 0; i < remaining; i++)
        {
            FrySliceUI slice = Instantiate(slicePrefab, vatArea);
            float x     = Random.Range(r.xMin * 0.7f, r.xMax * 0.7f);
            float y     = Random.Range(r.yMin * 0.7f, r.yMax * 0.7f);
            float angle = Random.Range(-30f, 30f);
            slice.transform.localPosition = new Vector3(x, y, 0f);
            slice.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            slice.Init(this);
            slice.SetCookingAnimation(true);
            _vatSlices.Add(slice);
        }
    }

    private void RebuildCollectedSlices()
    {
        foreach (FrySliceUI s in _collectedSlices)
            if (s != null) Destroy(s.gameObject);
        _collectedSlices.Clear();

        if (collectionArea == null || slicePrefab == null) return;

        for (int i = 0; i < _station.CollectedCount; i++)
        {
            FrySliceUI slice = Instantiate(slicePrefab, collectionArea);
            float x = Random.Range(-20f, 20f);
            float y = Random.Range(-20f, 20f);
            slice.transform.localPosition = new Vector3(x, y, 0f);
            slice.SetColor(ReadyColor);
            slice.enabled = false; // not interactive
            _collectedSlices.Add(slice);
        }
    }

    private void MoveSliceToCollection(FrySliceUI slice)
    {
        slice.transform.SetParent(collectionArea, false);
        float x = Random.Range(-20f, 20f);
        float y = Random.Range(-20f, 20f);
        //slice.transform.localPosition = new Vector3(x, y, 0f);
        slice.transform.DOLocalMove(new Vector3(x, y, 0f), 0.5f);
        slice.SetColor(ReadyColor);
        slice.SetCookingAnimation(false);
        slice.SetCollected(true);
        slice.enabled = false;
        _collectedSlices.Add(slice);
    }

    // ── Per-frame visual updates ─────────────────────────────────────────────

    private void RefreshVatColors()
    {
        if (_station.CurrentBatch == null) return;

        FryState state = _station.CurrentBatch.GetState();
        Color color;
        if (state == FryState.Ready)        color = ReadyColor;
        else if (state == FryState.Burnt)   color = BurntColor;
        else                                color = CookingColor;

        foreach (FrySliceUI s in _vatSlices)
            if (s != null) s.SetColor(color);
    }

    private void RefreshStatus()
    {
        if (statusText == null) return;

        if (_station.CurrentBatch == null)
        {
            if (_station.CollectedCount > 0)
                statusText.text = "Choose a sauce or finish plain";
            else
                statusText.text = _station.RawSliceStock > 0 ? "Start a batch" : "No stock — bring sliced potatoes";
            return;
        }

        FryerBatch batch = _station.CurrentBatch;
        FryState   state = batch.GetState();
        float    elapsed = batch.ElapsedTime;

        if (state == FryState.Cooking)
        {
            float remaining = batch.CookDuration - elapsed;
            statusText.text = $"Cooking... {remaining:F1}s";
        }
        else if (state == FryState.Ready)
        {
            float remaining = batch.ReadyEndTime - elapsed;
            statusText.text = $"Ready! Click to collect! {remaining:F1}s";
        }
        else
        {
            statusText.text = "Burnt! Clear the vat.";
        }
    }

    private void RefreshButtons()
    {
        bool noBatch     = _station.CurrentBatch == null;
        bool hasCollected = _station.CollectedCount > 0;

        // Can the player still click-to-collect fries?
        bool canCollectMore = !noBatch
            && _station.CurrentBatch.GetState() == FryState.Ready
            && _station.CurrentBatch.RemainingInVat > 0;

        bool hasVatRemains = !noBatch && _station.CurrentBatch.RemainingInVat > 0;

        // ── Small / Large: only when no batch and nothing collected ──────────
        bool showBatchButtons = noBatch && !hasCollected;
        if (smallButton != null)
        {
            smallButton.gameObject.SetActive(showBatchButtons);
            smallButton.interactable = _station.CanStartBatch(FryerBatch.Size.Small);
        }
        if (largeButton != null)
        {
            largeButton.gameObject.SetActive(showBatchButtons);
            largeButton.interactable = _station.CanStartBatch(FryerBatch.Size.Large);
        }

        // ── Sauce + Plain: after collecting, once no more fries can be collected
        bool showSaucePhase = hasCollected && !canCollectMore;
        if (ketchupButton != null)
        {
            ketchupButton.gameObject.SetActive(showSaucePhase);
            SetButtonHighlight(ketchupButton, _station.SelectedSauce == IngredientType.Ketchup);
        }
        if (mayoButton != null)
        {
            mayoButton.gameObject.SetActive(showSaucePhase);
            SetButtonHighlight(mayoButton, _station.SelectedSauce == IngredientType.Mayonnaise);
        }
        if (plainButton != null)
        {
            plainButton.gameObject.SetActive(showSaucePhase);
            SetButtonHighlight(plainButton, _station.SelectedSauce == IngredientType.None);
        }

        // ── Clear: visible when there are slices remaining in the vat ────────
        if (clearButton != null)
            clearButton.gameObject.SetActive(hasVatRemains);

        // ── Finish: available once sauce phase is active (sauce optional) ────
        if (finishButton != null)
            finishButton.gameObject.SetActive(showSaucePhase);

        // ── Stock text ───────────────────────────────────────────────────────
        RefreshStockText();
    }

    private static void SetButtonHighlight(Button btn, bool highlighted)
    {
        ColorBlock colors = btn.colors;
        colors.normalColor = highlighted ? Color.yellow : Color.white;
        btn.colors = colors;
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    private void ClearAllVisuals()
    {
        foreach (FrySliceUI s in _vatSlices)
            if (s != null) Destroy(s.gameObject);
        _vatSlices.Clear();

        foreach (FrySliceUI s in _collectedSlices)
            if (s != null) Destroy(s.gameObject);
        _collectedSlices.Clear();

        foreach (GameObject g in _trayVisuals)
            Destroy(g);
        _trayVisuals.Clear();
    }
}
