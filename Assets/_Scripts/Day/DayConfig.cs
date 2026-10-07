using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Editable configuration for the day/night cycle.
    /// Tweak these values in the Inspector to try different time windows easily.
    /// </summary>
    [CreateAssetMenu(fileName = "DayConfig", menuName = "Kosem Bufe/Day Config")]
    public class DayConfig : ScriptableObject
    {
        [Header("Time Window")]
        [Tooltip("Opening hour (7 = 07:00).")]
        [Range(0, 23)]
        public int openingHour = 7;

        [Tooltip("Opening minute.")]
        [Range(0, 59)]
        public int openingMinute = 0;

        [Tooltip("Closing hour (21 = 21:00).")]
        [Range(0, 23)]
        public int closingHour = 21;

        [Tooltip("Closing minute.")]
        [Range(0, 59)]
        public int closingMinute = 0;

        [Header("Prep Time")]
        [Tooltip("Prep minutes before auto-open. 0 = none. Skipped on day 1.")]
        [Min(0f)]
        public float prepTimeMinutes = 60f;

        [Header("Time Scale")]
        [Tooltip("Game minutes per real second.")]
        [Min(0.1f)]
        public float gameMinutesPerRealSecond = 2f;

        [Header("Customer Spawning")]
        [Tooltip("Real seconds between spawns.")]
        [Min(0.5f)]
        public float spawnIntervalSeconds = 3f;

        [Tooltip("Real seconds before the first spawn.")]
        [Min(0f)]
        public float firstSpawnDelay = 1f;

        /// <summary>Opening time expressed as a fraction of a day (0–1).</summary>
        public float OpeningTimeNormalized => (openingHour + openingMinute / 60f) / 24f;

        /// <summary>Closing time expressed as a fraction of a day (0–1).</summary>
        public float ClosingTimeNormalized => (closingHour + closingMinute / 60f) / 24f;

        /// <summary>Total in-game minutes in the work day.</summary>
        public float TotalDayMinutes =>
            (closingHour * 60 + closingMinute) - (openingHour * 60 + openingMinute);
    }
}
