using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public class PrepStation : MonoBehaviour, IAssemblyInteractable
{
    [Header("Setup")]
    [Tooltip("Position where the first ingredient is placed. Items stack upward from here.")]
    public Transform stackAnchor;

    // ── Runtime state ────────────────────────────────────────────────────────
    
    private PreparedItem _preparedItem;

    // ── IAssemblyInteractable ────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        if (!hand.IsHandEmpty && hand.HeldItem is PreparedItem)
            return _preparedItem == null;

        // TostBread is on the station — forward to its interaction logic.
        // Pickup (Closed state + empty hand) is also handled here so the bread's
        // own collider never needs to be enabled while it sits on the surface.
        if (_preparedItem != null)
        {
            if (hand.IsHandEmpty && _preparedItem.State == BreadState.Closed)
                return true; // player wants to pick up the finished bread
            return _preparedItem.CanInteract(hand);
        }

        // ── Original stacking logic ──────────────────────────────────────────

        // Holding an ingredient → can place (unless a finalized stack is waiting)
        if (!hand.IsHandEmpty)
        {
            if (_preparedItem.State == BreadState.Closed) return false;
            return hand.HeldItem is Ingredient;
        }
        
        return _preparedItem.State == BreadState.Closed;
    }

    public void Interact(PlayerHand hand)
    {
        // ── TostBread placement ──────────────────────────────────────────────
        if (!hand.IsHandEmpty && hand.HeldItem is PreparedItem bread && bread.State != BreadState.Toasted)
        {
            PlaceTostBread(bread, hand);
            return;
        }

        // ── TostBread on station: forward interaction or pick up ────────────
        if (_preparedItem != null)
        {
            if (hand.IsHandEmpty && _preparedItem.State == BreadState.Closed)
                PickUpTostBread(hand);
            else
                _preparedItem.Interact(hand);
            return;
        }

        // ── Holding an ingredient → place it on the stack ────────────────────
        if (!hand.IsHandEmpty)
        {
            Ingredient ingredient = hand.HeldItem as Ingredient;
            if (ingredient == null) return;

            PlaceIngredient(ingredient, hand);
            return;
        }

        // ── Empty hand interactions ──────────────────────────────────────────
        if (_preparedItem.State == BreadState.Closed)
        {
            PickUpFinalizedStack(hand);
        }
    }

    // ── TostBread placement ──────────────────────────────────────────────────

    private void PlaceTostBread(PreparedItem bread, PlayerHand hand)
    {
        hand.Release();

        bread.transform.SetParent(stackAnchor, worldPositionStays: false);
        bread.transform.localRotation = bread.OriginalLocalRotation;
        bread.transform.localScale    = bread.OriginalLocalScale;
        bread.transform.localPosition = Vector3.zero;

        // Colliders intentionally left DISABLED (same as regular ingredients).
        // All interactions go through the PrepStation's collider to avoid
        // physics overlap pushing the CharacterController on placement.

        _preparedItem = bread;
        bread.PlaceOnStation(this);

        Debug.Log("[PrepStation] TostBread placed on station.");
    }

    private void PickUpTostBread(PlayerHand hand)
    {
        hand.PickUp(_preparedItem);
    }

    /// <summary>Called by TostBread.OnPickedUp when the player lifts it off the station.</summary>
    public void OnPreparedItemPickedUp()
    {
        _preparedItem = null;
        Debug.Log("[PrepStation] TostBread removed from station.");
    }

    // ── Place an ingredient onto the stack ───────────────────────────────────

    private void PlaceIngredient(Ingredient ingredient, PlayerHand hand)
    {
        _preparedItem.AddIngredient(ingredient.ingredientType, hand);
        Debug.Log($"[PrepStation] Placed '{ingredient.itemName}' ({ingredient.ingredientType})");
    }
    

    // ── Pick up the finalized stack ─────────────────────────────────────────

    private void PickUpFinalizedStack(PlayerHand hand)
    {
        if (_preparedItem == null) return;

        // hand.PickUp reparents to holdPoint and disables physics/colliders.
        // The stack is already a child of stackAnchor with its collider
        // enabled, so no manual unparenting or collider toggling is needed.
        hand.PickUp(_preparedItem);

        _preparedItem = null;
    } }
}