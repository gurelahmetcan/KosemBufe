using UnityEngine;
using UnityEngine.UI;

namespace Bufe.UI
{
    /// <summary>
    /// Segmented relationship bar: a fixed row of 10 images that open/close to represent a
    /// 0–10 relationship value, tinted by level (low / medium / high). Used on the event
    /// screen to show the current speaker's relationship.
    /// </summary>
    public class RelationshipBarUI : MonoBehaviour
    {
        [Header("Segments (index 0 = first, 9 = last)")]
        [Tooltip("Exactly 10 segment images, ordered from first to last.")]
        public Image[] segments = new Image[10];

        [Header("Level Colors (assign your own)")]
        public Color lowColor    = new Color(0.85f, 0.20f, 0.20f, 1f);
        public Color mediumColor = new Color(0.85f, 0.75f, 0.10f, 1f);
        public Color highColor   = new Color(0.20f, 0.75f, 0.20f, 1f);

        /// <summary>
        /// Opens <paramref name="value"/> segments (0–10) and tints the open ones by level.
        /// Segments above the value are closed (hidden).
        /// </summary>
        public void SetValue(int value, RelationshipLevel level)
        {
            value = Mathf.Clamp(value, 0, 10);
            Color color = ColorForLevel(level);

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                bool open = i < value;
                segments[i].enabled = open;          // open / close
                if (open) segments[i].color = color; // tint by level
            }
        }

        private Color ColorForLevel(RelationshipLevel level)
        {
            switch (level)
            {
                case RelationshipLevel.Low:    return lowColor;
                case RelationshipLevel.Medium: return mediumColor;
                default:                       return highColor;
            }
        }
    }
}
