using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    /// <summary>
    /// HUD bar for a single faction's relationship (0–10), matching the segmented-bar look
    /// from the design board. Subscribes to FactionManager and recolours by level.
    ///
    /// Scene setup (segmented, preferred):
    ///   FactionBar (this)
    ///     ├─ Icon (Image, optional)
    ///     ├─ NameText (TMP, optional)
    ///     └─ Segments container → 10 child Images assigned to `segments`
    /// Or assign a single Filled `fillBar` Image instead for a continuous bar.
    /// </summary>
    public class FactionBarUI : MonoBehaviour
    {
        [Header("Faction")]
        [Tooltip("Which faction this bar represents. Resolved against FactionManager at runtime.")]
        public FactionData faction;

        [Header("References")]
        public FactionManager factionManager;
        public Image icon;
        public TextMeshProUGUI nameText;

        [Tooltip("Segmented bar cells (e.g. 10). Filled left-to-right by value. Leave empty to use fillBar.")]
        public List<Image> segments = new List<Image>();

        [Tooltip("Fallback continuous bar (type: Filled, Horizontal). Used only when `segments` is empty.")]
        public Image fillBar;

        [Header("Colors")]
        public Color highColor  = new Color(0.20f, 0.80f, 0.20f, 1f);
        public Color midColor   = new Color(0.95f, 0.78f, 0.20f, 1f);
        public Color lowColor   = new Color(0.90f, 0.25f, 0.20f, 1f);

        private void OnEnable()
        {
            if (factionManager == null) factionManager = FactionManager.Instance;
            if (factionManager != null)
                factionManager.OnRelationshipChanged += OnRelationshipChanged;

            BindVisuals();
            Refresh();
        }

        /// <summary>
        /// Re-points this bar at a different faction at runtime and refreshes.
        /// Used by the büfe order prompt, which reuses one bar instance for whichever
        /// faction member is at the counter.
        /// </summary>
        public void SetFaction(FactionData newFaction)
        {
            faction = newFaction;
            BindVisuals();
            Refresh();
        }

        private void BindVisuals()
        {
            if (faction == null) return;

            if (icon != null)
            {
                bool hasIcon = faction.icon != null;
                icon.gameObject.SetActive(hasIcon);
                if (hasIcon) icon.sprite = faction.icon;
            }
            if (nameText != null) nameText.text = faction.displayName.ToUpper();
        }

        private void OnDisable()
        {
            if (factionManager != null)
                factionManager.OnRelationshipChanged -= OnRelationshipChanged;
        }

        private void OnRelationshipChanged(FactionData changed, int oldVal, int newVal)
        {
            if (changed == faction) Refresh();
        }

        private void Refresh()
        {
            if (factionManager == null || faction == null) return;

            int   value    = Mathf.Clamp(factionManager.GetValue(faction), 0, 10);
            Color levelCol = LevelColor(factionManager.GetLevel(faction));

            if (segments != null && segments.Count > 0)
            {
                // Open `value` segments and tint them by level; close the rest —
                // same behaviour as RelationshipBarUI on the event screen.
                for (int i = 0; i < segments.Count; i++)
                {
                    if (segments[i] == null) continue;

                    bool open = i < value;
                    segments[i].enabled = open;
                    if (open) segments[i].color = levelCol;
                }
            }
            else if (fillBar != null)
            {
                fillBar.fillAmount = value / 10f;
                fillBar.color      = levelCol;
            }
        }

        private Color LevelColor(RelationshipLevel level) => level switch
        {
            RelationshipLevel.High   => highColor,
            RelationshipLevel.Medium => midColor,
            _                        => lowColor
        };
    }
}
