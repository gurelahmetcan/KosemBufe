using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core state machine for the toast preparation pipeline.
///
/// State flow:
///   Whole ──(knife click)──► Cut  ──(empty hand on station)──► Open
///   Whole ──(empty hand on station, Full size)──────────────► Open
///   Open  ──(ingredient in hand)──► ingredient added (stays Open)
///   Open  ──(empty hand)──────────► Closed
///   Closed ──(toaster)────────────► Toasting ──► Toasted
///
/// Interactions are routed here via IAssemblyInteractable.
/// PlayerHand picks this up as an InteractableItem when in Closed/Toasted state
/// and the player has an empty hand.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TostBread : InteractableItem, IAssemblyInteractable
{
    [Header("Placeholder Visuals")]
    [Tooltip("Active when bread is Whole (uncut).")]
    public GameObject wholeBreadVisual;

    [Tooltip("Active when bread has been cut in half.")]
    public GameObject halfBreadVisual;

    [Tooltip("Active when bread is open and ready for filling.")]
    public GameObject openBreadVisual;

    // ── Runtime state ─────────────────────────────────────────────────────────

    public BreadState State    { get; private set; } = BreadState.Whole;
    public BreadSize  Size     { get; private set; } = BreadSize.Full;
    public List<IngredientType> Ingredients { get; } = new List<IngredientType>();

    private PrepStation _station;
    private Toaster     _toaster;

    // ── Unity ─────────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        RefreshVisuals();
    }

    // ── IAssemblyInteractable ─────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // Knife + Whole → cut in half (works anywhere)
        if (hand.HeldItem is KnifeItem && State == BreadState.Whole)
            return true;

        // Everything below requires the bread to be resting on a station
        if (_station == null)
            return false;

        // Empty hand + Whole or Cut → open the bread
        if (hand.IsHandEmpty && (State == BreadState.Whole || State == BreadState.Cut))
            return true;

        // Ingredient in hand + Open → add to filling
        if (!hand.IsHandEmpty && hand.HeldItem is Ingredient && State == BreadState.Open)
            return true;

        // Empty hand + Open → close the bread
        if (hand.IsHandEmpty && State == BreadState.Open)
            return true;

        return false;
    }

    public void Interact(PlayerHand hand)
    {
        if (hand.HeldItem is KnifeItem && State == BreadState.Whole)
        {
            Cut();
            return;
        }

        if (hand.IsHandEmpty && (State == BreadState.Whole || State == BreadState.Cut))
        {
            Open();
            return;
        }

        if (hand.HeldItem is Ingredient ing && State == BreadState.Open)
        {
            AddIngredient(ing.ingredientType, hand);
            return;
        }

        if (hand.IsHandEmpty && State == BreadState.Open)
        {
            Close();
        }
    }

    // ── Called by PrepStation ─────────────────────────────────────────────────

    /// <summary>Registers which station this bread is currently resting on.</summary>
    public void PlaceOnStation(PrepStation station)
    {
        _station = station;
    }

    // ── Called by Toaster ─────────────────────────────────────────────────────

    /// <summary>Registers the toaster this bread is currently inside.</summary>
    public void PlaceInToaster(Toaster toaster)
    {
        _toaster = toaster;
    }

    // ── InteractableItem override ─────────────────────────────────────────────

    /// <summary>Notifies the station or toaster that this bread has been picked up.</summary>
    public override void OnPickedUp()
    {
        if (_station != null)
        {
            _station.OnTostBreadPickedUp();
            _station = null;
        }

        if (_toaster != null)
        {
            _toaster.OnTostBreadPickedUp();
            _toaster = null;
        }
    }

    // ── Called by Toaster ────────────────────────────────────────────────────

    public void StartToasting()
    {
        State = BreadState.Toasting;
        RefreshVisuals();
    }

    public void FinishToasting()
    {
        State = BreadState.Toasted;
        RefreshVisuals();
        Debug.Log("[TostBread] Toasted!");
    }

    // ── Private state transitions ─────────────────────────────────────────────

    private void Cut()
    {
        Size  = BreadSize.Half;
        State = BreadState.Cut;
        RefreshVisuals();
        Debug.Log("[TostBread] Cut to half.");
    }

    private void Open()
    {
        State = BreadState.Open;
        RefreshVisuals();
        Debug.Log($"[TostBread] Opened ({Size}).");
    }

    private void AddIngredient(IngredientType type, PlayerHand hand)
    {
        Ingredients.Add(type);
        hand.DestroyHeldItem();
        Debug.Log($"[TostBread] Added {type}. Filling: [{string.Join(", ", Ingredients)}]");
    }

    private void Close()
    {
        State = BreadState.Closed;
        RefreshVisuals();
        Debug.Log("[TostBread] Closed and ready for toaster.");
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    private void RefreshVisuals()
    {
        bool isWhole  = State == BreadState.Whole
                     || (State == BreadState.Closed  && Size == BreadSize.Full)
                     || (State == BreadState.Toasted && Size == BreadSize.Full);

        bool isHalf   = State == BreadState.Cut
                     || (State == BreadState.Closed  && Size == BreadSize.Half)
                     || (State == BreadState.Toasted && Size == BreadSize.Half);

        bool isOpen   = State == BreadState.Open;

        if (wholeBreadVisual != null) wholeBreadVisual.SetActive(isWhole);
        if (halfBreadVisual  != null) halfBreadVisual.SetActive(isHalf);
        if (openBreadVisual  != null) openBreadVisual.SetActive(isOpen);
        // Toasting state: all visuals off — bread is hidden inside the toaster
    }
}
