using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A grouped physical object created when a closing ingredient is placed
/// on a PrepStation stack. Contains all the stacked ingredients as children.
///
/// Created at runtime by PrepStation.Finalize(). The player picks this up
/// as a single unit and can deliver it to a customer or insert it into
/// the Toaster.
///
/// Individual Ingredient components on child objects retain their own
/// CookState (set by the Grill). The ToastState on this object is set
/// by the Toaster and applies to the whole stack.
/// </summary>

namespace Bufe
{
    public class FinalizedStack : InteractableItem
    {
        /// <summary>
        /// Quick-access list of ingredient types in this stack (bottom to top).
        /// Populated by PrepStation during finalization. Used for order validation.
        /// </summary>
        [HideInInspector]
        public List<Constants.IngredientType> ingredientTypes = new List<Constants.IngredientType>();

        /// <summary>
        /// Returns all Ingredient components in this stack (child objects).
        /// Use this to inspect individual cook states for validation.
        /// </summary>
        public Ingredient[] GetIngredients()
        {
            return GetComponentsInChildren<Ingredient>();
        }
    }
}