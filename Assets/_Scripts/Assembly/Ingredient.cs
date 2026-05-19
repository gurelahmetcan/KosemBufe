using Bufe;
using UnityEngine;

public class Ingredient : InteractableItem
{
    [Header("Ingredient Data")]
    [Tooltip("What type of ingredient this represents (used for order validation).")]
    public Constants.IngredientType ingredientType = Constants.IngredientType.None;
    
    [Header("Cooking State")]
    [Tooltip("Current cooking state of this ingredient.")]
    public Constants.CookState cookState = Constants.CookState.Raw;
}
