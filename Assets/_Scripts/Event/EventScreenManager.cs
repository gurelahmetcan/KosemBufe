using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bufe.UI;

namespace Bufe
{
    public class EventScreenManager : MonoBehaviour
    {
        public static EventScreenManager Instance { get; private set; }

        [Header("References")]
        public DayManager          dayManager;
        public MoneyManager        moneyManager;
        public RelationshipManager relationshipManager;
        public FamilyManager       familyManager;
        public EventScreenUI       eventScreenUI;
        public PlayerLook          playerLook;

        [Tooltip("Optional fade used to soften the day→night transition.")]
        public ScreenFader         screenFader;

        [Header("Event Pool")]
        [Tooltip("All authored events. Scheduled events (scheduledDay > 0) are handled automatically.")]
        public List<FamilyEvent> eventPool = new List<FamilyEvent>();

        [Header("Settings")]
        [Tooltip("Maximum number of events shown per day.")]
        [Min(1)] public int maxEventsPerDay = 3;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when all events for the day have been resolved. Passes today's day number.</summary>
        public event Action<int> OnEventPhaseComplete;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private readonly List<FamilyEvent>                    _todayQueue      = new List<FamilyEvent>();
        private readonly List<(GameEvent evt, int targetDay)> _scheduledEvents = new List<(GameEvent, int)>();
        private int _currentDay;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (dayManager      != null) dayManager.OnDayEnded           += HandleDayEnded;
            if (eventScreenUI   != null) eventScreenUI.OnChoiceSelected  += HandleChoiceSelected;
        }

        private void OnDisable()
        {
            if (dayManager      != null) dayManager.OnDayEnded           -= HandleDayEnded;
            if (eventScreenUI   != null) eventScreenUI.OnChoiceSelected  -= HandleChoiceSelected;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Schedules any event (night or day) for a future day, relative to the current in-game
        /// day. Called from the büfe day flow (OrderManager). Night events are processed at the
        /// next matching day end; day events are consumed by OrderManager on the matching day.
        /// </summary>
        public void ScheduleEvent(GameEvent evt, int delayDays)
        {
            if (evt == null) return;

            int baseDay   = dayManager != null ? dayManager.CurrentDay : _currentDay;
            int targetDay = baseDay + Mathf.Max(1, delayDays);

            _scheduledEvents.Add((evt, targetDay));
            Debug.Log($"[Event] Scheduled '{evt.name}' ({evt.Surface}) for day {targetDay}.");
        }

        /// <summary>
        /// Removes and returns the day-büfe events scheduled for the given day, for OrderManager
        /// to spawn at the counter. Night events are left in place for the day-end queue.
        /// </summary>
        public List<FactionInteraction> ConsumeDueDayEvents(int day)
        {
            var result = new List<FactionInteraction>();
            for (int i = _scheduledEvents.Count - 1; i >= 0; i--)
            {
                if (_scheduledEvents[i].targetDay != day) continue;
                if (_scheduledEvents[i].evt is not FactionInteraction dayEvt) continue;

                result.Add(dayEvt);
                _scheduledEvents.RemoveAt(i);
            }
            return result;
        }

        // ── Day flow ─────────────────────────────────────────────────────────────

        private void HandleDayEnded(int dayNumber)
        {
            if (screenFader != null)
                StartCoroutine(EnterNightRoutine(dayNumber));
            else
                EnterNight(dayNumber);
        }

        /// <summary>Fade through black into the night screen (events or day-end).</summary>
        private IEnumerator EnterNightRoutine(int dayNumber)
        {
            yield return screenFader.FadeOut();
            EnterNight(dayNumber);
            yield return screenFader.FadeIn();
        }

        private void EnterNight(int dayNumber)
        {
            _currentDay = dayNumber;
            BuildTodayQueue(dayNumber);

            if (_todayQueue.Count == 0)
            {
                OnEventPhaseComplete?.Invoke(_currentDay);
                return;
            }

            if (playerLook != null) playerLook.UIMode = true;
            ShowNext();
        }

        private void BuildTodayQueue(int dayNumber)
        {
            _todayQueue.Clear();

            // 1. Scheduled NIGHT events whose target day is today (day-büfe events are
            //    consumed separately by OrderManager via ConsumeDueDayEvents).
            for (int i = _scheduledEvents.Count - 1; i >= 0; i--)
            {
                var (evt, targetDay) = _scheduledEvents[i];
                if (targetDay != dayNumber) continue;
                if (evt is not FamilyEvent night) continue;
                if (!IsSpeakerAvailable(night)) continue;

                _todayQueue.Add(night);
                _scheduledEvents.RemoveAt(i);

                if (_todayQueue.Count >= maxEventsPerDay) return;
            }

            // 2. Authored pool: fixed-day events on their exact day, plus random events.
            foreach (var evt in eventPool)
            {
                if (_todayQueue.Count >= maxEventsPerDay) break;
                if (evt == null || evt.HasTriggered) continue;
                if (!IsSpeakerAvailable(evt)) continue;

                if (evt.scheduledDay > 0)
                {
                    // Fixed-day event: fires only on its scheduled day.
                    if (evt.scheduledDay == dayNumber)
                        _todayQueue.Add(evt);
                    continue;
                }

                // Random event.
                if (dayNumber < evt.minDay) continue;
                if (UnityEngine.Random.value > evt.triggerChance) continue;

                _todayQueue.Add(evt);
            }
        }

        private void ShowNext()
        {
            if (_todayQueue.Count == 0)
            {
                eventScreenUI?.Hide();
                if (playerLook != null) playerLook.UIMode = false;
                OnEventPhaseComplete?.Invoke(_currentDay);
                return;
            }

            var next = _todayQueue[0];
            _todayQueue.RemoveAt(0);
            next.HasTriggered = true;

            eventScreenUI?.Show(next, relationshipManager, familyManager);
        }

        // ── Choice processing ────────────────────────────────────────────────────

        private void HandleChoiceSelected(FamilyEvent evt, int choiceIndex)
        {
            var choice = evt.choices[choiceIndex];

            DecisionLog.Instance?.Add(choice.summaryLine);

            // Night has no counter to place items on, so no grant point is passed.
            EffectResolver.Apply(choice.effects, moneyManager, relationshipManager,
                                 FactionManager.Instance, itemGrantPoint: null, moneyReason: evt.name);

            // Schedule follow-up for a future day
            if (choice.followUpEvent != null)
                _scheduledEvents.Add((choice.followUpEvent, _currentDay + choice.followUpDelay));

            // EventScreenUI now plays animations and calls back when done
        }

        /// <summary>Called by EventScreenUI after its animations finish.</summary>
        public void NotifyAnimationComplete()
        {
            ShowNext();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private bool IsSpeakerAvailable(FamilyEvent evt)
        {
            if (evt.speaker == null) return true;

            foreach (var state in familyManager.States)
                if (state.Data == evt.speaker && state.IsGone) return false;

            return true;
        }
    }
}
