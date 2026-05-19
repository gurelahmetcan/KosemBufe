using System.Collections.Generic;
using Bufe;
using UnityEngine;

public static class OrderValidator
{
    
    /// <summary>
    /// Validate a prepared order against the target recipe + optional modifier.
    /// The modifier adds extra ingredients to the expected list before comparison.
    /// </summary>
    public static bool Validate(OrderItem recipe, List<Constants.IngredientType> preparedIngredients, Constants.CookState cookState)
    {
        // Build the expected ingredient list (base recipe + modifier)
        List<Constants.IngredientType> expected = BuildExpectedIngredients(recipe);

        Debug.Log($"[OrderValidator] Recipe='{recipe.itemName}'" +
                  $", expected=[{string.Join(", ", expected)}]" +
                  $", prepared=[{string.Join(", ", preparedIngredients)}]" +
                  $", cookState={cookState}");

        // ── 1. Ingredient check ──────────────────────────────────────────────
        if (!IngredientsMatch(expected, preparedIngredients))
            return false;

        // ── 2. Cooking check ─────────────────────────────────────────────────
        if (recipe.requiresCooking)
        {
            switch (cookState)
            {
                case Constants.CookState.Raw:   return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Builds the full expected ingredient list by combining the base recipe
    /// with any modifier additions.
    /// </summary>
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

    /// <summary>
    /// Order-insensitive comparison. Both lists must contain
    /// the same elements with the same counts.
    /// </summary>
    private static bool IngredientsMatch(List<Constants.IngredientType> required, List<Constants.IngredientType> prepared)
    {
        if (required == null || prepared == null) return false;
        if (required.Count != prepared.Count) return false;

        // Build a frequency map of the required ingredients
        var counts = new Dictionary<Constants.IngredientType, int>();

        foreach (Constants.IngredientType ing in required)
        {
            if (counts.ContainsKey(ing))
                counts[ing]++;
            else
                counts[ing] = 1;
        }

        // Subtract prepared ingredients
        foreach (Constants.IngredientType ing in prepared)
        {
            if (!counts.ContainsKey(ing))
                return false;

            counts[ing]--;
            if (counts[ing] < 0)
                return false;
        }

        return true;
    }
}
