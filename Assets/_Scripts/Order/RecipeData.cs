using Bufe;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "KosemBufe/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Tooltip("Display name shown above the customer (e.g. 'Tost').")]
    public string recipeName = "Tost";

    [Tooltip("How much the player earns for delivering this recipe.")]
    [Min(0f)]
    public float price = 10f;

    [Tooltip("Exact ingredients required. Order doesn't matter.")]
    public Constants.IngredientType[] ingredients;

    [Tooltip("If true, the order must be toasted (CookState.Cooked) to succeed.")]
    public bool requiresCooking = true;
}
