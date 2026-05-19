using Bufe;
using UnityEngine;

/// <summary>
/// A functional modifier that adds extra ingredients to an order.
/// When a customer requests this modifier, the expected ingredient list
/// is extended — the player must actually prepare the extra ingredient.
///
/// Example: "Extra Cheese" → adds 1× Cheese to the expected ingredients.
///
/// Create via: Assets → Create → KosemBufe → Order Modifier
/// </summary>
[CreateAssetMenu(fileName = "NewModifier", menuName = "KosemBufe/Order Modifier")]
public class OrderModifier : ScriptableObject
{
    [Tooltip("Internal name (e.g. 'ExtraCheese').")]
    public string modifierName;

    [Tooltip("How the customer phrases it (e.g. 'extra cheese please').")]
    public string displayText;

    [Header("Ingredient Effect")]
    [Tooltip("The ingredient to add to the expected order.")]
    public Constants.IngredientType ingredient;

    [Tooltip("How many of this ingredient to add (e.g. 1 for 'extra cheese').")]
    [Min(1)]
    public int quantity = 1;
}
