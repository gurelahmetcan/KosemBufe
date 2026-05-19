using System.Collections.Generic;
using Bufe;
using UnityEngine;

/// <summary>
/// Base component for any item the player can pick up and hold.
/// Attach this (and a Collider) to any source object in the scene.
///
/// For items that can also be sliced on the cutting board,
/// add a SliceableItem component alongside this one.
/// </summary>
public class HoldableItem : MonoBehaviour
{
    [Tooltip("Display name shown in the hand status UI (e.g. 'Sucuk', 'Ekmek').")]
    public string itemName = "Item";

    [Tooltip("Prefab instantiated at the hand display point when the player picks this up.")]
    public GameObject handPrefab;

    [Tooltip("Which ingredient this item represents (used for recipe validation).")]
    public Constants.IngredientType ingredient = Constants.IngredientType.None;
    
    // ── Prepared-order state (set by toaster flow) ───────────────────────────
    // These fields carry the full preparation data on toaster result items
    // so OrderManager can validate against a recipe.

    [HideInInspector] public List<Constants.IngredientType> preparedIngredients = new List<Constants.IngredientType>();
    [HideInInspector] public Constants.CookState cookState = Constants.CookState.Raw;
}
