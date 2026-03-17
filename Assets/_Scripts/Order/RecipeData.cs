using UnityEngine;

/// <summary>
/// Data asset defining a single recipe (menu item).
///
/// Create via: Assets → Create → Kosem Bufe → Recipe Data
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "KosemBufe/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Tooltip("Display name shown above the customer (e.g. 'Tost').")]
    public string recipeName = "Tost";

    [Tooltip("Exact ingredients required. Order doesn't matter.")]
    public Ingredient[] ingredients;

    [Tooltip("If true, the order must be toasted (CookState.Cooked) to succeed.")]
    public bool requiresCooking = true;
}
