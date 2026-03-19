using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static helper that compares a prepared order against a recipe,
/// optionally modified by an OrderModifier (e.g. "extra cheese").
///
/// Validation rules (checked in order):
///   1. Ingredient match — preparedIngredients must contain exactly the same
///      items as the (possibly modified) recipe (order-insensitive, duplicates count).
///   2. Cooking state — if the recipe requires cooking:
///        • CookState.Cooked → Perfect
///        • CookState.Raw    → Raw (fail)
///        • CookState.Burnt  → Burnt (fail)
/// </summary>
public static class OrderValidator
{
    /// <summary>
    /// Validate a prepared order against the target recipe (no modifier).
    /// </summary>
    public static OrderResult Validate(RecipeData recipe, List<Ingredient> preparedIngredients, CookState cookState)
    {
        return Validate(recipe, null, preparedIngredients, cookState);
    }

    /// <summary>
    /// Validate a prepared order against the target recipe + optional modifier.
    /// The modifier adds extra ingredients to the expected list before comparison.
    /// </summary>
    public static OrderResult Validate(RecipeData recipe, OrderModifier modifier, List<Ingredient> preparedIngredients, CookState cookState)
    {
        // Build the expected ingredient list (base recipe + modifier)
        List<Ingredient> expected = BuildExpectedIngredients(recipe, modifier);

        Debug.Log($"[OrderValidator] Recipe='{recipe.recipeName}'" +
                  (modifier != null ? $" + modifier '{modifier.modifierName}' ({modifier.quantity}× {modifier.ingredient})" : "") +
                  $", expected=[{string.Join(", ", expected)}]" +
                  $", prepared=[{string.Join(", ", preparedIngredients)}]" +
                  $", cookState={cookState}");

        // ── 1. Ingredient check ──────────────────────────────────────────────
        if (!IngredientsMatch(expected, preparedIngredients))
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
    /// Builds the full expected ingredient list by combining the base recipe
    /// with any modifier additions.
    /// </summary>
    private static List<Ingredient> BuildExpectedIngredients(RecipeData recipe, OrderModifier modifier)
    {
        var expected = new List<Ingredient>();

        if (recipe.ingredients != null)
        {
            for (int i = 0; i < recipe.ingredients.Length; i++)
                expected.Add(recipe.ingredients[i]);
        }

        if (modifier != null)
        {
            for (int i = 0; i < modifier.quantity; i++)
                expected.Add(modifier.ingredient);
        }

        return expected;
    }

    /// <summary>
    /// Order-insensitive comparison. Both lists must contain
    /// the same elements with the same counts.
    /// </summary>
    private static bool IngredientsMatch(List<Ingredient> required, List<Ingredient> prepared)
    {
        if (required == null || prepared == null) return false;
        if (required.Count != prepared.Count) return false;

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
