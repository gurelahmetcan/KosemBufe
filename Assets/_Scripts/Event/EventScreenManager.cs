using System;
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
        private readonly List<(FamilyEvent evt, int targetDay)> _scheduledEvents = new List<(FamilyEvent, int)>();
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

        // ── Day flow ─────────────────────────────────────────────────────────────

        private void HandleDayEnded(int dayNumber)
        {
            _currentDay = dayNumber;
            BuildTodayQueue(dayNumber);

            if (_todayQueue.Count == 0)
            {
                OnEventPhaseComplete?.Invoke(_currentDay);
                return;
            }

            ShowNext();
        }

        private void BuildTodayQueue(int dayNumber)
        {
            _todayQueue.Clear();

            // 1. Scheduled events whose target day is today
            for (int i = _scheduledEvents.Count - 1; i >= 0; i--)
            {
                var (evt, targetDay) = _scheduledEvents[i];
                if (targetDay != dayNumber) continue;
                if (!IsSpeakerAvailable(evt)) continue;

                _todayQueue.Add(evt);
                _scheduledEvents.RemoveAt(i);

                if (_todayQueue.Count >= maxEventsPerDay) return;
            }

            // 2. Random pool
            foreach (var evt in eventPool)
            {
                if (_todayQueue.Count >= maxEventsPerDay) break;
                if (evt.HasTriggered)    continue;
                if (evt.scheduledDay > 0) continue; // fixed-day events skip the random pool
                if (dayNumber < evt.minDay) continue;
                if (!IsSpeakerAvailable(evt)) continue;
                if (UnityEngine.Random.value > evt.triggerChance) continue;

                _todayQueue.Add(evt);
            }
        }

        private void ShowNext()
        {
            if (_todayQueue.Count == 0)
            {
                eventScreenUI?.Hide();
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

            foreach (var effect in choice.effects)
            {
                // Money
                if (effect.moneyDelta != 0f && moneyManager != null)
                {
                    if (effect.moneyDelta < 0f)
                        moneyManager.DeductExpense(-effect.moneyDelta, evt.name);
                    else
                        moneyManager.AddIncome(effect.moneyDelta);
                }

                // Relationship
                if (effect.targetMember != null && effect.relationshipDelta != 0)
                    relationshipManager?.Modify(effect.targetMember, effect.relationshipDelta);
            }

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
