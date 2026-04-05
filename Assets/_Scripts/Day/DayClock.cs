using System;
using UnityEngine;

/// <summary>
/// Simulates an in-game clock that advances from a start time toward a closing time.
/// Time is expressed in "minutes since midnight" internally and converted to hours:minutes for display.
/// The clock only ticks while explicitly running (controlled by DayManager).
/// </summary>
public class DayClock : MonoBehaviour
{
    /// <summary>Fired every frame the clock ticks. Passes (currentHour, currentMinute).</summary>
    public event Action<int, int> OnTimeChanged;

    /// <summary>Fired once when the clock reaches the closing time.</summary>
    public event Action OnClosingTimeReached;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private float _currentTimeMinutes;   // minutes since midnight (fractional)
    private float _closingTimeMinutes;
    private float _gameMinutesPerRealSecond;
    private bool  _ticking;
    private bool  _closingFired;

    /// <summary>Current hour (0–23).</summary>
    public int CurrentHour => Mathf.FloorToInt(_currentTimeMinutes / 60f) % 24;

    /// <summary>Current minute (0–59).</summary>
    public int CurrentMinute => Mathf.FloorToInt(_currentTimeMinutes % 60f);

    /// <summary>True while the clock is advancing.</summary>
    public bool IsTicking => _ticking;

    /// <summary>True if closing time has been reached or passed.</summary>
    public bool PastClosingTime => _currentTimeMinutes >= _closingTimeMinutes;

    /// <summary>
    /// How far through the work day we are (0 = opening, 1 = closing).
    /// </summary>
    public float DayProgress(float openingMinutes)
    {
        float range = _closingTimeMinutes - openingMinutes;
        if (range <= 0f) return 1f;
        return Mathf.Clamp01((_currentTimeMinutes - openingMinutes) / range);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initialise the clock to a specific time. Does not start ticking.
    /// </summary>
    public void SetTime(int hour, int minute, float closingTimeMinutes, float gameMinutesPerRealSecond)
    {
        _currentTimeMinutes        = hour * 60f + minute;
        _closingTimeMinutes        = closingTimeMinutes;
        _gameMinutesPerRealSecond  = gameMinutesPerRealSecond;
        _closingFired              = false;
        _ticking                   = false;
    }

    /// <summary>Start the clock ticking.</summary>
    public void StartTicking()
    {
        _ticking = true;
    }

    /// <summary>Pause the clock.</summary>
    public void StopTicking()
    {
        _ticking = false;
    }

    // ── Unity loop ───────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_ticking) return;

        _currentTimeMinutes += _gameMinutesPerRealSecond * Time.deltaTime;

        OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);

        if (!_closingFired && _currentTimeMinutes >= _closingTimeMinutes)
        {
            _closingFired = true;
            _ticking = false;
            // Clamp to exact closing time for clean display
            _currentTimeMinutes = _closingTimeMinutes;
            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
            OnClosingTimeReached?.Invoke();
        }
    }
}
