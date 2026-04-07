using System;
using UnityEngine;

/// <summary>
/// Central state machine for the day cycle, inspired by Papers Please.
///
/// States:
///   PreOpen   → It's morning. Clock shows opening time. Player hasn't opened the shutter yet.
///   Open      → Shutter opened. Clock ticks. Customers spawn on an interval.
///   Closing   → Clock hit closing time. No new customers. Current order can be finished.
///   EndOfDay  → All orders done. Player can click the door to end the day.
///
/// Flow:
///   PreOpen  ──(click shutter)──►  Open  ──(closing time)──►  Closing
///   Closing  ──(last customer done)──►  EndOfDay  ──(click door)──►  PreOpen (next day)
/// </summary>
public class DayManager : MonoBehaviour
{
    public enum DayPhase
    {
        PreOpen,
        Open,
        Closing,
        EndOfDay
    }

    [Header("References")]
    [Tooltip("Configuration asset with all editable time values.")]
    public DayConfig config;

    [Tooltip("Clock component that tracks in-game time.")]
    public DayClock clock;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired when the day phase changes. Passes the new phase.</summary>
    public event Action<DayPhase> OnPhaseChanged;

    /// <summary>Fired when the shutter is opened and the shop is ready for customers.</summary>
    public event Action OnDayStarted;

    /// <summary>Fired when closing time is reached — no more customer spawns.</summary>
    public event Action OnClosingTimeReached;

    /// <summary>Fired when the player ends the day (clicks door). Passes the day number that just ended.</summary>
    public event Action<int> OnDayEnded;

    /// <summary>Fired on the spawn-interval tick while the shop is open. OrderManager listens to this.</summary>
    public event Action OnCustomerSpawnTick;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private DayPhase _currentPhase;
    private int      _currentDay = 1;
    private float    _spawnTimer;
    private bool     _firstSpawnPending;
    private bool     _hasActiveCustomer;

    /// <summary>Current phase of the day.</summary>
    public DayPhase CurrentPhase => _currentPhase;

    /// <summary>Current day number (1-based).</summary>
    public int CurrentDay => _currentDay;

    /// <summary>True when the shop is open and customers should be spawning.</summary>
    public bool IsOpenForCustomers => _currentPhase == DayPhase.Open;

    /// <summary>True when we are past closing but finishing the last customer.</summary>
    public bool IsClosing => _currentPhase == DayPhase.Closing;

    /// <summary>True when the player can click the door to end the day.</summary>
    public bool CanEndDay => _currentPhase == DayPhase.EndOfDay;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("[DayManager] DayConfig is not assigned!", this);
            return;
        }

        if (clock == null)
        {
            Debug.LogError("[DayManager] DayClock is not assigned!", this);
            return;
        }

        clock.OnClosingTimeReached += HandleClosingTime;
        BeginNewDay();
    }

    private void OnDestroy()
    {
        if (clock != null)
            clock.OnClosingTimeReached -= HandleClosingTime;
    }

    private void Update()
    {
        if (_currentPhase != DayPhase.Open) return;

        // Handle first-spawn delay
        if (_firstSpawnPending)
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _firstSpawnPending = false;
                _spawnTimer = 0f;
                OnCustomerSpawnTick?.Invoke();
            }
            return;
        }

        // Regular spawn interval
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = config.spawnIntervalSeconds;
            OnCustomerSpawnTick?.Invoke();
        }
    }

    // ── Public API (called by interactables / OrderManager) ──────────────────

    /// <summary>
    /// Called when the player clicks the shutter to open the büfe.
    /// Transitions from PreOpen → Open.
    /// </summary>
    public void OpenShutter()
    {
        if (_currentPhase != DayPhase.PreOpen) return;

        SetPhase(DayPhase.Open);
        clock.StartTicking();

        _firstSpawnPending = true;
        _spawnTimer = config.firstSpawnDelay;

        Debug.Log($"[DayManager] Day {_currentDay} — shutter opened! Customers incoming.");
        OnDayStarted?.Invoke();
    }

    /// <summary>
    /// Called by OrderManager to inform the DayManager whether there is an active customer.
    /// When closing and no active customer remains, we move to EndOfDay.
    /// </summary>
    public void SetActiveCustomer(bool hasCustomer)
    {
        _hasActiveCustomer = hasCustomer;

        if (_currentPhase == DayPhase.Closing && !_hasActiveCustomer)
        {
            SetPhase(DayPhase.EndOfDay);
            Debug.Log("[DayManager] Last customer served. Click the door to end the day.");
        }
    }

    /// <summary>
    /// Called when the player clicks the door to end the day.
    /// Transitions from EndOfDay → PreOpen (next day).
    /// </summary>
    public void EndDay()
    {
        if (_currentPhase != DayPhase.EndOfDay) return;

        int dayThatEnded = _currentDay;
        _currentDay++;

        Debug.Log($"[DayManager] Day {dayThatEnded} ended. Starting day {_currentDay}.");
        OnDayEnded?.Invoke(dayThatEnded);

        BeginNewDay();
    }

    // ── Debug API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Forces the day to end immediately, regardless of current phase.
    /// Stops the clock, fires closing + end-of-day events, and starts next day.
    /// </summary>
    public void ForceEndDay()
    {
        if (clock != null) clock.StopTicking();

        // If we were Open, fire closing so OrderManager cleans up
        if (_currentPhase == DayPhase.Open || _currentPhase == DayPhase.PreOpen)
            OnClosingTimeReached?.Invoke();

        int dayThatEnded = _currentDay;
        _currentDay++;

        Debug.Log($"[DayManager] DEBUG — forced end of day {dayThatEnded}.");
        OnDayEnded?.Invoke(dayThatEnded);

        BeginNewDay();
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void BeginNewDay()
    {
        float closingMinutes = config.closingHour * 60f + config.closingMinute;
        clock.SetTime(config.openingHour, config.openingMinute, closingMinutes, config.gameMinutesPerRealSecond);

        _hasActiveCustomer = false;
        SetPhase(DayPhase.PreOpen);

        Debug.Log($"[DayManager] Day {_currentDay} — it's {config.openingHour:D2}:{config.openingMinute:D2}. Open the shutter to begin.");
    }

    private void HandleClosingTime()
    {
        Debug.Log($"[DayManager] It's {config.closingHour:D2}:{config.closingMinute:D2} — closing time!");
        OnClosingTimeReached?.Invoke();

        if (_hasActiveCustomer)
        {
            SetPhase(DayPhase.Closing);
            Debug.Log("[DayManager] Finish the current customer, then click the door.");
        }
        else
        {
            SetPhase(DayPhase.EndOfDay);
            Debug.Log("[DayManager] No active customer. Click the door to end the day.");
        }
    }

    private void SetPhase(DayPhase newPhase)
    {
        if (_currentPhase == newPhase) return;
        _currentPhase = newPhase;
        OnPhaseChanged?.Invoke(_currentPhase);
    }
}
