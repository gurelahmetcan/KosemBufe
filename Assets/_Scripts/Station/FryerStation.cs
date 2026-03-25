using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deep fryer station. Manages raw potato-slice stock, a single frying
/// batch with a background timer, and output of finished french-fry portions.
///
/// Interaction (via IInteractable):
///   - Sliced fryable item in hand → deposit slices into stock.
///   - Empty hand                  → open the fryer UI panel.
///
/// The batch timer runs every frame (Update) even while the UI is closed,
/// so fries keep cooking while the player handles other orders.
///
/// Scene Setup:
///   1. Place a GameObject with a Collider in the scene.
///   2. Add this component. Assign fryerUI, output prefabs.
///   3. On the Potato prefab: set supportedProcesses = Fryable, add SliceableItem.
/// </summary>
public class FryerStation : Station
{
    [Header("UI")]
    [Tooltip("The FryerUI panel controller in the scene.")]
    public FryerUI fryerUI;

    [Header("Output Prefabs")]
    [Tooltip("HoldableItem prefab for a finished small portion (no mesh/collider needed).")]
    public HoldableItem smallFriesPrefab;

    [Tooltip("HoldableItem prefab for a finished large portion (no mesh/collider needed).")]
    public HoldableItem largeFriesPrefab;

    [Header("Settings")]
    [Tooltip("How many raw slices one whole potato yields when deposited.")]
    public int slicesPerPotato = 12;

    [Header("Timing")]
    [Tooltip("Seconds the cooking phase lasts (fries not yet collectable).")]
    public float cookDuration = 10f;

    [Tooltip("Seconds after which ready fries become burnt (must be > cookDuration).")]
    public float readyEndTime = 15f;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private int         _rawSliceStock;
    private FryerBatch  _currentBatch;
    private int         _collectedCount;
    private Ingredient  _selectedSauce = Ingredient.None;

    // ── Public read-only state (used by FryerUI) ─────────────────────────────

    public int         RawSliceStock  => _rawSliceStock;
    public FryerBatch  CurrentBatch   => _currentBatch;
    public int         CollectedCount => _collectedCount;
    public Ingredient  SelectedSauce  => _selectedSauce;

    // ── Station interface ────────────────────────────────────────────────────

    public override bool CanInteract(InHandController hand)
    {
        // Holding sliced fryable item → deposit
        if (hand.HeldItem != null
            && hand.IsSliced
            && hand.HeldItem.supportedProcesses.HasFlag(ProcessType.Fryable))
            return true;

        // Empty hand → open UI
        if (hand.HeldItem == null)
            return true;

        return false;
    }

    public override void Interact(InHandController hand)
    {
        // ── Deposit stock ────────────────────────────────────────────────────
        if (hand.HeldItem != null
            && hand.IsSliced
            && hand.HeldItem.supportedProcesses.HasFlag(ProcessType.Fryable))
        {
            _rawSliceStock += slicesPerPotato;
            hand.DestroyHeldItem();
            Debug.Log($"[Fryer] Deposited {slicesPerPotato} slices. Stock: {_rawSliceStock}");

            if (fryerUI != null && fryerUI.IsOpen)
                fryerUI.Refresh();
            return;
        }

        // ── Open UI ──────────────────────────────────────────────────────────
        if (hand.HeldItem == null && fryerUI != null)
        {
            hand.InputBlocked = true;
            fryerUI.Open(this, hand);
        }
    }

    // ── Background timer ─────────────────────────────────────────────────────

    private void Update()
    {
        if (_currentBatch != null)
            _currentBatch.Tick(Time.deltaTime);
    }

    // ── Batch operations (called by FryerUI) ─────────────────────────────────

    public bool CanStartBatch(FryerBatch.Size size)
    {
        return _currentBatch == null
            && _collectedCount == 0
            && _rawSliceStock >= (int)size;
    }

    public void StartBatch(FryerBatch.Size size)
    {
        int slices = (int)size;
        _rawSliceStock  -= slices;
        _currentBatch    = new FryerBatch(size, cookDuration, readyEndTime);
        _collectedCount  = 0;
        _selectedSauce   = Ingredient.None;
        Debug.Log($"[Fryer] Started {size} batch ({slices} slices). Stock remaining: {_rawSliceStock}");
    }

    public bool TryCollectSlice()
    {
        if (_currentBatch == null) return false;
        if (_currentBatch.GetState() != FryState.Ready) return false;
        if (_currentBatch.RemainingInVat <= 0) return false;

        _currentBatch.CollectOne();
        _collectedCount++;
        Debug.Log($"[Fryer] Collected slice. Total collected: {_collectedCount}, remaining in vat: {_currentBatch.RemainingInVat}");
        return true;
    }

    public void ClearVat()
    {
        if (_currentBatch == null) return;

        Debug.Log($"[Fryer] Clearing vat. Remaining: {_currentBatch.RemainingInVat}");

        if (_collectedCount == 0)
        {
            // Nothing collected — discard entire batch
            _currentBatch = null;
        }
        else
        {
            // Keep batch alive for the sauce/finish phase
            _currentBatch.ClearRemaining();
        }
    }

    public void SelectSauce(Ingredient sauce)
    {
        _selectedSauce = sauce;
        Debug.Log($"[Fryer] Selected sauce: {sauce}");
    }

    /// <summary>
    /// Creates the output HoldableItem and places it in the player's hand.
    /// </summary>
    public bool Finish(InHandController hand)
    {
        if (_collectedCount == 0)
            return false;

        HoldableItem prefab = _currentBatch != null && _currentBatch.BatchSize == FryerBatch.Size.Small
            ? smallFriesPrefab
            : largeFriesPrefab;

        // Fallback: if batch was already nulled (all collected + cleared), infer from count
        if (_currentBatch == null)
            prefab = _collectedCount <= (int)FryerBatch.Size.Small ? smallFriesPrefab : largeFriesPrefab;

        if (prefab == null)
        {
            Debug.LogError("[Fryer] Missing output prefab!", this);
            return false;
        }

        HoldableItem item = Instantiate(prefab);

        // Build ingredients — sauce is optional (plain fries have no sauce)
        var ingredients = new List<Ingredient> { Ingredient.FrenchFries };
        if (_selectedSauce != Ingredient.None)
            ingredients.Add(_selectedSauce);

        item.preparedIngredients = ingredients;
        item.cookState = CookState.Cooked;

        hand.PickUp(item);

        Debug.Log($"[Fryer] Finished! {_collectedCount} fries with {(_selectedSauce != Ingredient.None ? _selectedSauce.ToString() : "no sauce")}");

        _currentBatch   = null;
        _collectedCount = 0;
        _selectedSauce  = Ingredient.None;

        return true;
    }
}
