using UnityEngine;

/// <summary>
/// Data asset defining a single recipe (menu item).
///
/// Modifiers that are valid for this recipe are listed here
/// (not on the archetype) so that only sensible combinations
/// can be generated (e.g. Ketchup for fries, ExtraCheese for tost).
///
/// Create via: Assets → Create → Kosem Bufe → Recipe Data
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "KosemBufe/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Tooltip("Display name shown above the customer (e.g. 'Tost').")]
    public string recipeName = "Tost";

    [Tooltip("How much the player earns for delivering this recipe.")]
    [Min(0f)]
    public float price = 10f;

    [Tooltip("Exact ingredients required. Order doesn't matter.")]
    public Ingredient[] ingredients;

    [Tooltip("If true, the order must be toasted (CookState.Cooked) to succeed.")]
    public bool requiresCooking = true;

    [Header("Modifiers")]
    [Tooltip("Modifiers valid for this recipe (e.g. Ketchup/Mayo for fries, ExtraCheese for tost). " +
             "Leave empty if this recipe has no modifiers.")]
    public OrderModifier[] possibleModifiers;

    [Tooltip("Chance (0–1) that a customer adds a modifier when ordering this recipe.")]
    [Range(0f, 1f)]
    public float modifierChance = 0.3f;

    /// <summary>
    /// Optionally selects a modifier based on modifierChance.
    /// Returns null if no modifier is applied or the list is empty.
    /// </summary>
    public OrderModifier PickModifier()
    {
        if (possibleModifiers == null || possibleModifiers.Length == 0) return null;
        if (Random.value > modifierChance) return null;
        return possibleModifiers[Random.Range(0, possibleModifiers.Length)];
    }
}
