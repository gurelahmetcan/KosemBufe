using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physical prep surface (cutting board / assembly area) where the player
/// stacks ingredients one by one to build a sandwich or other grouped item.
///
/// LIFO Stack Rules:
///   - Player holds an Ingredient → left-click station to push it onto the stack.
///   - Empty hand + stack NOT finalized → left-click to Pop the top ingredient back.
///   - When an Ingredient with isClosingBread is placed (and stack has 2+ items),
///     the stack is finalized into a FinalizedStack grouped object.
///   - Empty hand + stack IS finalized → left-click to pick up the whole group.
///
/// Toast Flow (TostBread):
///   - Player holds TostBread → left-click station to place it on the surface.
///     Bread colliders stay DISABLED while on the station (same as regular ingredients)
///     to prevent the CharacterController from being pushed on placement.
///   - All bread interactions (knife/cut, open, fill, close, pickup) are forwarded
///     through the PrepStation's own collider, not the bread's.
///   - PrepStation is blocked for regular stack interactions while a TostBread is on it.
///
/// </summary>
namespace Bufe
{
    public class PrepStation : MonoBehaviour, IAssemblyInteractable
{
    [Header("Setup")]
    [Tooltip("Position where the first ingredient is placed. Items stack upward from here.")]
    public Transform stackAnchor;

    [Tooltip("Vertical distance between stacked ingredients.")]
    public float stackOffset = 0.05f;

    // ── Runtime state ────────────────────────────────────────────────────────

    private readonly List<Ingredient> _stack = new List<Ingredient>();
    private FinalizedStack _finalizedStack;
    private bool _isFinalized;
    private TostBread _tostBread;

    // ── Public read-only state ───────────────────────────────────────────────

    public int  StackCount  => _stack.Count;
    public bool IsFinalized => _isFinalized;
    public bool IsEmpty     => _stack.Count == 0 && !_isFinalized && _tostBread == null;

    // ── IAssemblyInteractable ────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // ── TostBread placement ──────────────────────────────────────────────
        if (!hand.IsHandEmpty && hand.HeldItem is TostBread)
            return _tostBread == null && _stack.Count == 0 && !_isFinalized;

        // TostBread is on the station — forward to its interaction logic.
        // Pickup (Closed state + empty hand) is also handled here so the bread's
        // own collider never needs to be enabled while it sits on the surface.
        if (_tostBread != null)
        {
            if (hand.IsHandEmpty && _tostBread.State == BreadState.Closed)
                return true; // player wants to pick up the finished bread
            return _tostBread.CanInteract(hand);
        }

        // ── Original stacking logic ──────────────────────────────────────────

        // Holding an ingredient → can place (unless a finalized stack is waiting)
        if (!hand.IsHandEmpty)
        {
            if (_isFinalized) return false;
            return hand.HeldItem is Ingredient;
        }

        // Empty hand → can pop from incomplete stack or pick up finalized stack
        return _stack.Count > 0 || _isFinalized;
    }

    public void Interact(PlayerHand hand)
    {
        // ── TostBread placement ──────────────────────────────────────────────
        if (!hand.IsHandEmpty && hand.HeldItem is TostBread bread)
        {
            PlaceTostBread(bread, hand);
            return;
        }

        // ── TostBread on station: forward interaction or pick up ────────────
        if (_tostBread != null)
        {
            if (hand.IsHandEmpty && _tostBread.State == BreadState.Closed)
                PickUpTostBread(hand);
            else
                _tostBread.Interact(hand);
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
        if (_isFinalized)
        {
            PickUpFinalizedStack(hand);
        }
        else if (_stack.Count > 0)
        {
            PopTopIngredient(hand);
        }
    }

    // ── TostBread placement ──────────────────────────────────────────────────

    private void PlaceTostBread(TostBread bread, PlayerHand hand)
    {
        hand.Release();

        bread.transform.SetParent(stackAnchor, worldPositionStays: false);
        bread.transform.localRotation = bread.OriginalLocalRotation;
        bread.transform.localScale    = bread.OriginalLocalScale;
        bread.transform.localPosition = Vector3.zero;

        // Colliders intentionally left DISABLED (same as regular ingredients).
        // All interactions go through the PrepStation's collider to avoid
        // physics overlap pushing the CharacterController on placement.

        _tostBread = bread;
        bread.PlaceOnStation(this);

        Debug.Log("[PrepStation] TostBread placed on station.");
    }

    private void PickUpTostBread(PlayerHand hand)
    {
        // PlayerHand.PickUp uses GetComponentsInChildren which finds disabled
        // colliders, so no pre-enabling is needed here.
        hand.PickUp(_tostBread);
        // TostBread.OnPickedUp fires and calls OnTostBreadPickedUp below.
    }

    /// <summary>Called by TostBread.OnPickedUp when the player lifts it off the station.</summary>
    public void OnTostBreadPickedUp()
    {
        _tostBread = null;
        Debug.Log("[PrepStation] TostBread removed from station.");
    }

    // ── Place an ingredient onto the stack ───────────────────────────────────

    private void PlaceIngredient(Ingredient ingredient, PlayerHand hand)
    {
        // Release from hand (item stays dormant: kinematic, colliders disabled).
        // Release unparents the item.
        hand.Release();

        // Parent under stackAnchor so the stack follows the board if it moves.
        // stackAnchor MUST have a uniform (1,1,1) lossy scale — place it as a
        // child of an unscaled board root so parenting cannot distort the
        // ingredient's size. Rotation and scale are restored from the prefab's
        // authored values; position is stacked upward along the anchor's local
        // Y so the stack follows the board's orientation.
        ingredient.transform.SetParent(stackAnchor, worldPositionStays: false);
        ingredient.transform.localRotation = ingredient.OriginalLocalRotation;
        ingredient.transform.localScale    = ingredient.OriginalLocalScale;
        ingredient.transform.localPosition = new Vector3(0f, stackOffset * _stack.Count, 0f);

        _stack.Add(ingredient);

        Debug.Log($"[PrepStation] Placed '{ingredient.itemName}' ({ingredient.ingredientType})" +
                  $" → stack size: {_stack.Count}");
    }

    // ── Pop the top ingredient (LIFO error correction) ──────────────────────

    private void PopTopIngredient(PlayerHand hand)
    {
        int topIndex = _stack.Count - 1;
        Ingredient top = _stack[topIndex];
        _stack.RemoveAt(topIndex);

        // Unparent from station
        top.transform.SetParent(null);

        // Re-enable colliders so PickUp can cache them
        foreach (Collider col in top.GetComponentsInChildren<Collider>())
            col.enabled = true;

        hand.PickUp(top);

        Debug.Log($"[PrepStation] Popped '{top.itemName}' → stack size: {_stack.Count}");
    }

    // ── Pick up the finalized stack ─────────────────────────────────────────

    private void PickUpFinalizedStack(PlayerHand hand)
    {
        if (_finalizedStack == null) return;

        // hand.PickUp reparents to holdPoint and disables physics/colliders.
        // The stack is already a child of stackAnchor with its collider
        // enabled, so no manual unparenting or collider toggling is needed.
        hand.PickUp(_finalizedStack);

        Debug.Log($"[PrepStation] Picked up finalized stack '{_finalizedStack.itemName}'");

        _finalizedStack = null;
        _isFinalized = false;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string BuildStackName()
    {
        if (_stack.Count == 0) return "Stack";

        // Use the name of the bottom item as base (e.g. "Bread Stack")
        return $"{_stack[0].itemName} Stack";
    }

    /// <summary>
    /// Calculates a BoxCollider that fits all child Renderers.
    /// </summary>
    private static void FitBoxColliderToChildren(GameObject root, BoxCollider box)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size   = root.transform.InverseTransformVector(bounds.size);
    }
}
}