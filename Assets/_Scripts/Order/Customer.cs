using UnityEngine;
using TMPro;

/// <summary>
/// Customer placeholder that enters the scene and waits for the player to
/// take and deliver an order.
///
/// States:
///   WAITING_FOR_ORDER → Click to take the order (starts the timer).
///   WAITING_FOR_FOOD  → Click again while holding the order to deliver.
///
/// Scene Setup:
///  1. Attach to a GameObject with a SpriteRenderer and a Collider.
///  2. Assign orderLabel – a child TextMeshPro (world-space) that shows the
///     order name above the customer. Starts disabled.
/// </summary>
public class Customer : MonoBehaviour
{
    public enum State { WaitingForOrder, WaitingForFood }

    [Header("UI")]
    [Tooltip("World-space TextMeshPro label shown above the customer after ordering.")]
    public TextMeshPro orderLabel;

    // ── Public state ─────────────────────────────────────────────────────────

    public State CurrentState { get; private set; } = State.WaitingForOrder;

    /// <summary>The recipe assigned to this customer (set by OrderManager).</summary>
    public RecipeData Recipe { get; private set; }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns a recipe to the customer and transitions to WaitingForFood.
    /// Called by OrderManager when the player clicks this customer.
    /// </summary>
    public void AssignOrder(RecipeData recipe)
    {
        Recipe = recipe;
        CurrentState = State.WaitingForFood;

        if (orderLabel != null)
        {
            orderLabel.text = recipe.recipeName;
            orderLabel.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Resets the customer to its initial state.
    /// </summary>
    public void ResetCustomer()
    {
        CurrentState = State.WaitingForOrder;
        Recipe = null;

        if (orderLabel != null)
            orderLabel.gameObject.SetActive(false);
    }
}
