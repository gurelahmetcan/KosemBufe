using UnityEngine;

/// <summary>
/// Defines how a customer talks based on their current mood.
/// Each mood provides its own pool of order phrases, modifier phrases,
/// and exit lines.
///
/// Create via: Assets → Create → KosemBufe → Customer Mood
/// </summary>
[CreateAssetMenu(fileName = "NewMood", menuName = "KosemBufe/Customer Mood")]
public class CustomerMood : ScriptableObject
{
    [Tooltip("Mood identifier (e.g. 'tired', 'friendly', 'grumpy').")]
    public string moodName;

    [Header("Ordering")]
    [Tooltip("How they phrase their order. Use {0} for the food name.")]
    [TextArea(1, 3)]
    public string[] orderPhrases;

    [Tooltip("How they phrase a modifier. Use {0} for the modifier text.")]
    [TextArea(1, 3)]
    public string[] modifierPhrases;

    [Header("Exit Lines")]
    [Tooltip("Exit line.")]
    [TextArea(1, 3)]
    public string[] exitLines;
    
    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a random line from the given array, or a fallback if empty.
    /// </summary>
    private static string Pick(string[] pool, string fallback = "...")
    {
        if (pool == null || pool.Length == 0) return fallback;
        return pool[Random.Range(0, pool.Length)];
    }

    public string GetOrderPhrase()      => Pick(orderPhrases, "I'll have a {0}.");
    public string GetModifierPhrase()   => Pick(modifierPhrases, "Oh, and {0}.");
    public string GetExitLine()     => Pick(exitLines, "Thanks!");
}
