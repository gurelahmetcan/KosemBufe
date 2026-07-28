using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Marker placed on every item that is spawned at runtime during play
    /// (ingredients, premade pickups, sucuk slices, cut bread halves, event
    /// grants). Items placed in the scene at edit time (display props, storages,
    /// decor) never receive this marker, so they are left untouched.
    ///
    /// At the end of a day <see cref="ClearAll"/> destroys every tagged item,
    /// wiping the counter/floor clean for the next morning.
    /// </summary>
    public class DailySpawn : MonoBehaviour
    {
        private static readonly HashSet<DailySpawn> _all = new HashSet<DailySpawn>();

        private void OnEnable()  => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>Adds the marker to a runtime-spawned object (no-op if already tagged or null).</summary>
        public static GameObject Tag(GameObject go)
        {
            if (go != null && go.GetComponent<DailySpawn>() == null)
                go.AddComponent<DailySpawn>();
            return go;
        }

        /// <summary>
        /// Destroys every currently tagged item, except quest items (see
        /// <see cref="QuestItem"/>) which persist across days. Called on the day reset.
        /// </summary>
        public static void ClearAll()
        {
            // Copy first: destroying triggers OnDisable which mutates the set.
            var snapshot = new List<DailySpawn>(_all);
            foreach (var spawn in snapshot)
            {
                if (spawn == null) { _all.Remove(spawn); continue; }

                // Quest/story items are exempt — they carry over to future days.
                if (spawn.GetComponent<QuestItem>() != null) continue;

                _all.Remove(spawn);
                Destroy(spawn.gameObject);
            }
        }
    }
}
