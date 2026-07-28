using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Marks an item as belonging to a quest/story chain (e.g. the taxi package).
    ///
    /// Two effects:
    ///   • The daily reset skips quest items, so they survive across days instead of
    ///     being cleared like ordinary leftover pickups (see <see cref="DailySpawn"/>).
    ///   • An event choice can consume the item from anywhere in the world by id
    ///     (see <see cref="DestroyById"/>) — no need for the player to be holding it.
    ///
    /// Author this component on the quest item's prefab and give it a unique
    /// <see cref="questId"/> (e.g. "taxi_box").
    /// </summary>
    public class QuestItem : MonoBehaviour
    {
        [Tooltip("Unique id used to find/consume this item from an event choice (e.g. 'taxi_box').")]
        public string questId;

        private static readonly HashSet<QuestItem> _all = new HashSet<QuestItem>();

        private void OnEnable()  => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>Destroys every live quest item with the given id. Returns how many were destroyed.</summary>
        public static int DestroyById(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return 0;

            int destroyed = 0;
            foreach (var item in new List<QuestItem>(_all))
            {
                if (item != null && item.questId == questId)
                {
                    Destroy(item.gameObject);
                    destroyed++;
                }
            }
            return destroyed;
        }
    }
}
