using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Records the player's notable event decisions across a playthrough as short summary
    /// lines (e.g. "Sent your son on the school trip") so the demo-end screen can recap them.
    /// Lives in the GameScene — a fresh instance starts empty each new game.
    /// </summary>
    public class DecisionLog : MonoBehaviour
    {
        public static DecisionLog Instance { get; private set; }

        private readonly List<string> _entries = new List<string>();
        public IReadOnlyList<string> Entries => _entries;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Records a decision summary line (blank lines are ignored).</summary>
        public void Add(string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                _entries.Add(line);
        }

        public void Clear() => _entries.Clear();
    }
}
