using UnityEngine;
using TMPro;

/// <summary>
/// Displays the in-game clock as "HH:MM" and the current day number.
///
/// Scene Setup:
///   Canvas (Screen Space – Overlay)
///     └─ ClockPanel
///         └─ ClockText   (TextMeshProUGUI, e.g. "14:35")
///         └─ DayText     (TextMeshProUGUI, e.g. "Day 3")  [optional]
///         └─ StatusText  (TextMeshProUGUI, e.g. "CLOSED")  [optional]
/// </summary>
public class DayClockUI : MonoBehaviour
{
    [Header("References")]
    public DayClock clock;
    public DayManager dayManager;

    [Header("UI Elements")]
    [Tooltip("Text showing the current time (HH:MM).")]
    public TextMeshProUGUI clockText;

    [Tooltip("Optional text showing the day number.")]
    public TextMeshProUGUI dayText;

    [Tooltip("Optional text showing the current status (e.g. CLOSED, OPEN).")]
    public TextMeshProUGUI statusText;

    private void OnEnable()
    {
        if (clock != null)
            clock.OnTimeChanged += OnTimeChanged;

        if (dayManager != null)
            dayManager.OnPhaseChanged += OnPhaseChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (clock != null)
            clock.OnTimeChanged -= OnTimeChanged;

        if (dayManager != null)
            dayManager.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnTimeChanged(int hour, int minute)
    {
        if (clockText != null)
            clockText.text = $"{hour:D2}:{minute:D2}";
    }

    private void OnPhaseChanged(DayManager.DayPhase phase)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (clock != null && clockText != null)
            clockText.text = $"{clock.CurrentHour:D2}:{clock.CurrentMinute:D2}";

        if (dayManager != null)
        {
            if (dayText != null)
                dayText.text = $"Day {dayManager.CurrentDay}";

            if (statusText != null)
            {
                statusText.text = dayManager.CurrentPhase switch
                {
                    DayManager.DayPhase.PreOpen  => "CLOSED",
                    DayManager.DayPhase.Open     => "OPEN",
                    DayManager.DayPhase.Closing  => "CLOSING",
                    DayManager.DayPhase.EndOfDay => "END OF DAY",
                    _                            => ""
                };
            }
        }
    }
}
