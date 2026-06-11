using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public enum RelationshipLevel { Low, Medium, High }

    public class RelationshipManager : MonoBehaviour
    {
        public static RelationshipManager Instance { get; private set; }

        [Header("Family Members")]
        [Tooltip("All family members whose relationships are tracked.")]
        public List<FamilyMemberData> members = new List<FamilyMemberData>();

        [Header("Starting Values")]
        [Tooltip("Default relationship value for every member at game start.")]
        [Range(0, 10)] public int defaultRelationship = 7;

        [Header("Level Thresholds")]
        [Tooltip("0 to this value = Low (Red).")]
        [Range(0, 10)] public int lowMaxThreshold    = 3;
        [Tooltip("lowMaxThreshold+1 to this value = Medium (Yellow). Above = High (Green).")]
        [Range(0, 10)] public int mediumMaxThreshold = 6;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when a member's relationship value changes. (member, oldValue, newValue)</summary>
        public event Action<FamilyMemberData, int, int> OnRelationshipChanged;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private readonly Dictionary<FamilyMemberData, int> _values =
            new Dictionary<FamilyMemberData, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var member in members)
                _values[member] = defaultRelationship;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public int GetValue(FamilyMemberData member)
        {
            return _values.TryGetValue(member, out int val) ? val : 0;
        }

        public RelationshipLevel GetLevel(FamilyMemberData member)
        {
            int val = GetValue(member);
            if (val <= lowMaxThreshold)    return RelationshipLevel.Low;
            if (val <= mediumMaxThreshold) return RelationshipLevel.Medium;
            return RelationshipLevel.High;
        }

        /// <summary>Apply a delta to a member's relationship value. Clamps to 0–10.</summary>
        public void Modify(FamilyMemberData member, int delta)
        {
            if (!_values.ContainsKey(member)) return;

            int oldVal = _values[member];
            int newVal = Mathf.Clamp(oldVal + delta, 0, 10);

            if (newVal == oldVal) return;

            _values[member] = newVal;
            OnRelationshipChanged?.Invoke(member, oldVal, newVal);
        }
    }
}
