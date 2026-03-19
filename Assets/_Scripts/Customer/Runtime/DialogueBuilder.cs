/// <summary>
/// Pure static utility that assembles dialogue strings from a CustomerInstance.
/// No MonoBehaviour, no side effects — just data in, strings out.
///
/// Normal customer flow:
///   1. Opening line   → shown when customer spawns
///   2. Order line     → shown when player clicks customer (+ optional modifier)
///   3. Exit line      → shown after delivery or timeout (positive / negative / timeout)
/// </summary>
public static class DialogueBuilder
{
    /// <summary>
    /// The order dialogue shown when the player clicks the customer.
    /// Contains the order phrase and an optional modifier phrase.
    /// </summary>
    public struct OrderDialogue
    {
        public string order;
        public string modifier; // Empty string if no modifier
    }

    /// <summary>
    /// Returns the opening line for when the customer spawns.
    /// </summary>
    public static string GetOpeningLine(CustomerInstance instance)
    {
        return instance.Mood.GetOpeningLine();
    }

    /// <summary>
    /// Builds the order dialogue shown when the player clicks the customer.
    /// </summary>
    public static OrderDialogue BuildOrderDialogue(CustomerInstance instance)
    {
        var mood = instance.Mood;

        string orderPhrase = mood.GetOrderPhrase();
        orderPhrase = orderPhrase.Replace("{0}", instance.Order.recipeName);

        string modifierPhrase = "";
        if (instance.HasModifier)
        {
            modifierPhrase = mood.GetModifierPhrase();
            modifierPhrase = modifierPhrase.Replace("{0}", instance.Modifier.displayText);
        }

        return new OrderDialogue
        {
            order    = orderPhrase,
            modifier = modifierPhrase
        };
    }

    /// <summary>
    /// Returns the appropriate exit line based on the order result.
    /// Perfect → positive exit, WrongIngredients/Raw/Burnt → negative exit.
    /// </summary>
    public static string GetExitLine(CustomerInstance instance, OrderResult result)
    {
        var mood = instance.Mood;

        if (result == OrderResult.Perfect)
            return mood.GetPositiveExit();

        return mood.GetNegativeExit();
    }

    /// <summary>
    /// Returns the timeout exit line (patience expired or delivery timer expired).
    /// </summary>
    public static string GetTimeoutExitLine(CustomerInstance instance)
    {
        return instance.Mood.GetTimeoutExit();
    }
}
