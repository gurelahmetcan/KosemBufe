using UnityEngine;

namespace Bufe
{
    public class Ingredient : InteractableItem
    {
        [Header("Ingredient Data")]
        [Tooltip("Ingredient type, used for order validation.")]
        public Constants.IngredientType ingredientType = Constants.IngredientType.None;
    }
}
