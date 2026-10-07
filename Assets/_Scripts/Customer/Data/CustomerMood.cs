using UnityEngine;

namespace Bufe
{
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
        [Tooltip("Mood id (e.g. 'tired').")]
        public string moodName;

        [Header("Ordering")]
        [Tooltip("Order line. {0} = food name.")]
        [TextArea(1, 3)]
        public string[] orderPhrases;

        [Tooltip("Modifier line. {0} = modifier.")]
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
}
