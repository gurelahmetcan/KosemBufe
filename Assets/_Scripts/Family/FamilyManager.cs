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

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when any family member's derived status changes after a day ends.</summary>
        public event Action<FamilyMemberState> OnMemberStatusChanged;

        /// <summary>Fired when a family member's IsGone becomes true.</summary>
        public event Action<FamilyMemberState> OnMemberGone;

        // ── Runtime ──────────────────────────────────────────────────────────────

        public IReadOnlyList<FamilyMemberState> States => _states;
        private readonly List<FamilyMemberState> _states = new List<FamilyMemberState>();

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
        /// Processes the end of a day. Call this from DayEndScreen before BeginNextDay.
        /// medicatedMembers: the set of FamilyMemberState instances whose medicine was paid.
        /// </summary>
        public void ProcessDayEnd(bool foodPaid, bool heatPaid, HashSet<FamilyMemberState> medicatedMembers)
        {
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
