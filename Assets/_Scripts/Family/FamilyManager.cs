using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public class FamilyManager : MonoBehaviour
    {
        public static FamilyManager Instance { get; private set; }

        [Header("Family Members")]
        public List<FamilyMemberData> members = new List<FamilyMemberData>();

        [Header("Surprise Events")]
        public List<SurpriseEvent> surpriseEventPool = new List<SurpriseEvent>();

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when any family member's derived status changes after a day ends.</summary>
        public event Action<FamilyMemberState> OnMemberStatusChanged;

        /// <summary>Fired when a family member's IsGone becomes true.</summary>
        public event Action<FamilyMemberState> OnMemberGone;

        // ── Runtime ──────────────────────────────────────────────────────────────

        public IReadOnlyList<FamilyMemberState> States => _states;
        private readonly List<FamilyMemberState> _states = new List<FamilyMemberState>();

        private SurpriseEvent _todaysSurpriseEvent;
        private bool _surpriseRolledThisDay;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var data in members)
                _states.Add(new FamilyMemberState(data));
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the surprise event for today, rolling once per day.
        /// Returns null if no event triggers or the pool is empty.
        /// </summary>
        public SurpriseEvent GetTodaysSurpriseEvent(int dayNumber)
        {
            if (_surpriseRolledThisDay) return _todaysSurpriseEvent;

            _surpriseRolledThisDay = true;
            _todaysSurpriseEvent   = null;

            var eligible = new List<SurpriseEvent>();
            foreach (var e in surpriseEventPool)
            {
                if (dayNumber >= e.minDay && UnityEngine.Random.value < e.chance)
                    eligible.Add(e);
            }

            if (eligible.Count > 0)
                _todaysSurpriseEvent = eligible[UnityEngine.Random.Range(0, eligible.Count)];

            return _todaysSurpriseEvent;
        }

        /// <summary>
        /// Processes the end of a day. Call this from DayEndScreen before BeginNextDay.
        /// medicatedMembers: the set of FamilyMemberState instances whose medicine was paid.
        /// </summary>
        public void ProcessDayEnd(bool foodPaid, bool heatPaid, HashSet<FamilyMemberState> medicatedMembers)
        {
            _surpriseRolledThisDay = false;
            _todaysSurpriseEvent   = null;

            foreach (var state in _states)
            {
                if (state.IsGone) continue;

                var prevStatus   = state.Status;
                bool medPaid     = medicatedMembers != null && medicatedMembers.Contains(state);

                state.ProcessDayEnd(foodPaid, heatPaid, medPaid);

                if (state.Status != prevStatus)
                    OnMemberStatusChanged?.Invoke(state);

                if (state.IsGone)
                    OnMemberGone?.Invoke(state);
            }
        }
    }
}
