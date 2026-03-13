using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static helper that compares a prepared order against a recipe.
///
/// Validation rules (checked in order):
///   1. Ingredient match — preparedIngredients must contain exactly the same
///      items as the recipe (order-insensitive, duplicates count).
///   2. Cooking state — if the recipe requires cooking:
///        • CookState.Cooked → Perfect
///        • CookState.Raw    → Raw (fail)
///        • CookState.Burnt  → Burnt (fail)
/// </summary>
public static class OrderValidator
{
    /// <summary>
    /// Validate a prepared order against the target recipe.
    /// </summary>
    public static OrderResult Validate(RecipeData recipe, List<Ingredient> preparedIngredients, CookState cookState)
    {
        Debug.Log($"[OrderValidator] Recipe='{recipe.recipeName}', required=[{string.Join(", ", recipe.ingredients)}], prepared=[{string.Join(", ", preparedIngredients)}], cookState={cookState}");

        // ── 1. Ingredient check ──────────────────────────────────────────────
        if (!IngredientsMatch(recipe.ingredients, preparedIngredients))
            return OrderResult.WrongIngredients;

        // ── 2. Cooking check ─────────────────────────────────────────────────
        if (recipe.requiresCooking)
        {
            switch (cookState)
            {
                case CookState.Raw:   return OrderResult.Raw;
                case CookState.Burnt: return OrderResult.Burnt;
            }
        }

        return OrderResult.Perfect;
    }

    /// <summary>
    /// Order-insensitive comparison. Both lists must contain
    /// the same elements with the same counts.
    /// </summary>
    private static bool IngredientsMatch(Ingredient[] required, List<Ingredient> prepared)
    {
        if (required == null || prepared == null) return false;
        if (required.Length != prepared.Count) return false;

        // Build a frequency map of the required ingredients
        var counts = new Dictionary<Ingredient, int>();

        foreach (Ingredient ing in required)
        {
            if (counts.ContainsKey(ing))
                counts[ing]++;
            else
                counts[ing] = 1;
        }

        // Subtract prepared ingredients
        foreach (Ingredient ing in prepared)
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
