using UnityEngine;

/// <summary>
/// Editable configuration for the day/night cycle.
/// Tweak these values in the Inspector to try different time windows easily.
/// </summary>
[CreateAssetMenu(fileName = "DayConfig", menuName = "Kosem Bufe/Day Config")]
public class DayConfig : ScriptableObject
{
    [Header("Time Window")]
    [Tooltip("Hour the clock starts at (e.g. 7 = 07:00).")]
    [Range(0, 23)]
    public int openingHour = 7;

    [Tooltip("Minute the clock starts at.")]
    [Range(0, 59)]
    public int openingMinute = 0;

    [Tooltip("Hour when customers stop arriving (e.g. 21 = 21:00).")]
    [Range(0, 23)]
    public int closingHour = 21;

    [Tooltip("Minute when customers stop arriving.")]
    [Range(0, 59)]
    public int closingMinute = 0;

    [Header("Time Scale")]
    [Tooltip("How many in-game minutes pass per real-time second. " +
             "E.g. 2 means 1 real second = 2 in-game minutes.")]
    [Min(0.1f)]
    public float gameMinutesPerRealSecond = 2f;

    [Header("Customer Spawning")]
    [Tooltip("Seconds (real-time) between automatic customer spawns while open.")]
    [Min(0.5f)]
    public float spawnIntervalSeconds = 3f;

    [Tooltip("Delay (real-time seconds) after opening before the first customer spawns.")]
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
