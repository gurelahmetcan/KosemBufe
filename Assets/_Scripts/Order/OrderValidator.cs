using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public static class OrderValidator
    {
        /// <summary>
        /// Checks a held item against a single order slot.
        /// Premade items match by identity; prepared items match by ingredients and cook state.
        /// </summary>
        public static bool ValidateSlot(OrderSlot slot, InteractableItem heldItem)
        {
            if (slot.target.category == Constants.ItemCategory.Premade)
            {
                PremadeItem premade = heldItem.GetComponent<PremadeItem>();
                return premade != null && premade.definition == slot.target;
            }
            else
            {
                if (heldItem is not PreparedItem prepared) return false;
                return ValidatePrepared(slot.target, prepared.Ingredients, prepared.cookState);
            }
        }

        /// <summary>
        /// Validates a prepared item's ingredients and cook state against a recipe.
        /// </summary>
        private static bool ValidatePrepared(OrderItem recipe, IReadOnlyList<Constants.IngredientType> preparedIngredients, Constants.CookState cookState)
        {
            List<Constants.IngredientType> expected = BuildExpectedIngredients(recipe);

            Debug.Log($"[OrderValidator] Recipe='{recipe.itemName}'" +
                      $", expected=[{string.Join(", ", expected)}]" +
                      $", prepared=[{string.Join(", ", preparedIngredients)}]" +
                      $", cookState={cookState}");

            if (!IngredientsMatch(expected, preparedIngredients))
                return false;

            if (recipe.requiresCooking && cookState == Constants.CookState.Raw)
                return false;

            return true;
        }

        private static List<Constants.IngredientType> BuildExpectedIngredients(OrderItem recipe)
        {
            var expected = new List<Constants.IngredientType>();
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Length; i++)
                    expected.Add(recipe.ingredients[i]);
            }
            return expected;
        }

        private static bool IngredientsMatch(List<Constants.IngredientType> required, IReadOnlyList<Constants.IngredientType> prepared)
        {
            if (required == null || prepared == null) return false;
            if (required.Count != prepared.Count) return false;

            var counts = new Dictionary<Constants.IngredientType, int>();
            foreach (Constants.IngredientType ing in required)
            {
                if (counts.ContainsKey(ing)) counts[ing]++;
                else counts[ing] = 1;
            }

            foreach (Constants.IngredientType ing in prepared)
            {
                if (!counts.ContainsKey(ing)) return false;
                counts[ing]--;
                if (counts[ing] < 0) return false;
            }

            return true;
        }
    }
}
