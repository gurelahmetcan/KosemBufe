using UnityEngine;

/// <summary>
/// A physical ingredient that exists in the game world and can be picked up,
/// placed on the PrepStation stack, or inserted into cooking appliances.
///
/// Key properties:
///   - ingredientType: What kind of ingredient this is (Bread, Sucuk, etc.)
///   - isClosingBread: When true, placing this on a PrepStation finalizes the stack
///   - cookState:      Tracks cooking progress (Raw, Cooked, Toasted, Burnt)
///
/// Attach a Collider alongside this component for raycast detection.
/// </summary>
public class Ingredient : InteractableItem
{
    [Header("Ingredient Data")]
    [Tooltip("What type of ingredient this represents (used for order validation).")]
    public IngredientType ingredientType = IngredientType.None;

    [Tooltip("When true, placing this ingredient on a PrepStation closes/finalizes the stack, " +
             "grouping all stacked items into a single pickable object.")]
    public bool isClosingBread;

    [Header("Cooking State")]
    [Tooltip("Current cooking state of this ingredient.")]
    public CookState cookState = CookState.Raw;

    [Header("Visual Variants")]
    [Tooltip("Optional prefab shown in hand after this ingredient has been cooked/grilled.")]
    public GameObject cookedHandPrefab;

    /// <summary>
    /// Returns the appropriate hand prefab based on the current cook state.
    /// Falls back to the default handPrefab if no cooked variant is assigned.
    /// </summary>
    public GameObject GetCurrentHandPrefab()
    {
        if (cookState != CookState.Raw && cookedHandPrefab != null)
            return cookedHandPrefab;
        return handPrefab;
    }
}
