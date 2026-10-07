using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Tracks the player's shop-wide relationship (0–10) with each faction.
    /// Mirrors RelationshipManager (family) deliberately - for the July 29 demo the two
    /// stay parallel; post-demo they merge into one generic manager
    /// (see Docs/FactionSystemDesign.md §3).
    ///
    /// Reuses the RelationshipLevel enum (Low/Medium/High) defined in RelationshipManager.cs.
    /// </summary>
    public class FactionManager : MonoBehaviour
    {
        public static FactionManager Instance { get; private set; }

        [Header("Factions")]
        [Tooltip("Tracked factions.")]
        public List<FactionData> factions = new List<FactionData>();

        [Header("Level Thresholds")]
        [Tooltip("Up to this = Low (red).")]
        [Range(0, 10)] public int lowMaxThreshold = 3;
        [Tooltip("Up to this = Medium (yellow). Above = High.")]
        [Range(0, 10)] public int mediumMaxThreshold = 6;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when a faction's relationship value changes. (faction, oldValue, newValue)</summary>
        public event Action<FactionData, int, int> OnRelationshipChanged;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private readonly Dictionary<FactionData, int>  _values = new Dictionary<FactionData, int>();
        private readonly Dictionary<FactionId, FactionData> _byId = new Dictionary<FactionId, FactionData>();

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var faction in factions)
            {
                if (faction == null) continue;
                _values[faction] = faction.startingRelationship;
                _byId[faction.id] = faction;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public FactionData GetFaction(FactionId id)
        {
            return _byId.TryGetValue(id, out var faction) ? faction : null;
        }

        public int GetValue(FactionData faction)
        {
            return faction != null && _values.TryGetValue(faction, out int val) ? val : 0;
        }

        public int GetValue(FactionId id) => GetValue(GetFaction(id));

        public RelationshipLevel GetLevel(FactionData faction)
        {
            int val = GetValue(faction);
            if (val <= lowMaxThreshold)    return RelationshipLevel.Low;
            if (val <= mediumMaxThreshold) return RelationshipLevel.Medium;
            return RelationshipLevel.High;
        }

        public RelationshipLevel GetLevel(FactionId id) => GetLevel(GetFaction(id));

        /// <summary>Apply a delta to a faction's relationship value. Clamps to 0–10.</summary>
        public void Modify(FactionData faction, int delta)
        {
            if (faction == null || !_values.ContainsKey(faction)) return;

            int oldVal = _values[faction];
            int newVal = Mathf.Clamp(oldVal + delta, 0, 10);

            if (newVal == oldVal) return;

            _values[faction] = newVal;
            Log.Info($"[Faction] {faction.displayName} {oldVal} → {newVal} ({(delta > 0 ? "+" : "")}{delta})");
            OnRelationshipChanged?.Invoke(faction, oldVal, newVal);
        }

        public void Modify(FactionId id, int delta) => Modify(GetFaction(id), delta);
    }
}
