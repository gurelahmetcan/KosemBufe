/// <summary>
/// Pure static utility that assembles dialogue strings from a CustomerInstance.
/// No MonoBehaviour, no side effects — just data in, strings out.
/// </summary>
public static class DialogueBuilder
{
    /// <summary>
    /// Holds the full set of dialogue lines for a normal customer visit.
    /// </summary>
    public struct NormalDialogue
    {
        public string opening;
        public string order;
        public string modifier; // Empty string if no modifier
        public string exit;
    }

    /// <summary>
    /// Builds a complete dialogue set for a normal customer visit.
    /// </summary>
    public static NormalDialogue BuildNormal(CustomerInstance instance)
    {
        var mood = instance.Mood;

        string orderPhrase = mood.GetOrderPhrase();
        // Replace {0} with the actual food name
        orderPhrase = orderPhrase.Replace("{0}", instance.Order.recipeName);

        string modifierPhrase = "";
        if (instance.HasModifier)
        {
            modifierPhrase = mood.GetModifierPhrase();
            modifierPhrase = modifierPhrase.Replace("{0}", instance.Modifier.displayText);
        }

        return new NormalDialogue
        {
            opening  = mood.GetOpeningLine(),
            order    = orderPhrase,
            modifier = modifierPhrase,
            exit     = mood.GetExitLine()
        };
    }

    /// <summary>
    /// Returns the full dialogue as a single combined string.
    /// Useful for simple display in a single text bubble.
    /// </summary>
    public static string BuildNormalCombined(CustomerInstance instance)
    {
        NormalDialogue d = BuildNormal(instance);

        if (string.IsNullOrEmpty(d.modifier))
            return $"{d.opening}\n{d.order}";

        return $"{d.opening}\n{d.order}\n{d.modifier}";
    }

    /// <summary>
    /// Returns a mood-appropriate reaction to the order result.
    /// </summary>
    public static string GetReaction(CustomerInstance instance, OrderResult result)
    {
        return instance.Mood.GetReaction(result);
    }

    /// <summary>
    /// Returns the exit line for the customer.
    /// </summary>
    public static string GetExitLine(CustomerInstance instance)
    {
        return instance.Mood.GetExitLine();
    }
}
