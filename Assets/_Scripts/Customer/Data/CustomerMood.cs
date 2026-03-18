using UnityEngine;

/// <summary>
/// Defines how a customer talks based on their current mood.
/// Each mood provides its own pool of opening lines, order phrases,
/// modifier phrases, exit lines, and reactions.
///
/// Create via: Assets → Create → KosemBufe → Customer Mood
/// </summary>
[CreateAssetMenu(fileName = "NewMood", menuName = "KosemBufe/Customer Mood")]
public class CustomerMood : ScriptableObject
{
    [Tooltip("Mood identifier (e.g. 'tired', 'friendly', 'grumpy').")]
    public string moodName;

    [Header("Dialogue Lines")]
    [Tooltip("Greetings when the customer arrives.")]
    [TextArea(1, 3)]
    public string[] openingLines;

    [Tooltip("How they phrase their order. Use {0} for the food name.")]
    [TextArea(1, 3)]
    public string[] orderPhrases;

    [Tooltip("How they phrase a modifier. Use {0} for the modifier text.")]
    [TextArea(1, 3)]
    public string[] modifierPhrases;

    [Tooltip("What they say when leaving.")]
    [TextArea(1, 3)]
    public string[] exitLines;

    [Header("Reactions")]
    [Tooltip("Reaction lines per order result (Perfect, WrongIngredients, Raw, Burnt).")]
    public ReactionLine[] reactions;

    /// <summary>
    /// Returns a random line from the given array, or a fallback if empty.
    /// </summary>
    private static string Pick(string[] pool, string fallback = "...")
    {
        if (pool == null || pool.Length == 0) return fallback;
        return pool[Random.Range(0, pool.Length)];
    }

    public string GetOpeningLine()  => Pick(openingLines, "Hey.");
    public string GetOrderPhrase()  => Pick(orderPhrases, "I'll have a {0}.");
    public string GetModifierPhrase() => Pick(modifierPhrases, "Oh, and {0}.");
    public string GetExitLine()     => Pick(exitLines, "Thanks.");

    /// <summary>
    /// Returns a random reaction line for the given order result.
    /// </summary>
    public string GetReaction(OrderResult result)
    {
        if (reactions == null) return "...";

        for (int i = 0; i < reactions.Length; i++)
        {
            if (reactions[i].result == result)
                return Pick(reactions[i].lines, "...");
        }

        return "...";
    }
}
