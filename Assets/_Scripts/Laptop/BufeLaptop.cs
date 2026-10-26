using Bufe.UI;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// The laptop on the büfe counter. Clicking it opens the management panel. Locked before
    /// <see cref="unlockDay"/> so it stays out of the day-1 intro/tutorial.
    /// Needs a Collider so the player's raycast can hit it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BufeLaptop : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [Header("References")]
        public LaptopPanel panel;
        public DayManager  dayManager;

        [Header("Settings")]
        [Tooltip("First usable day. 1 for testing.")]
        [Min(1)] public int unlockDay = 2;

        [SerializeField] private string hoverLabelText = "Laptop";

        private bool IsUnlocked => dayManager == null || dayManager.CurrentDay >= unlockDay;

        private void Awake()
        {
            if (panel      == null) panel      = FindAnyObjectByType<LaptopPanel>(FindObjectsInactive.Include);
            if (dayManager == null) dayManager = FindAnyObjectByType<DayManager>();
        }

        // ── IAssemblyInteractable ────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand) => panel != null && !panel.IsOpen && IsUnlocked;

        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if (AudioManager.Instance) AudioManager.Instance.PlaySfx(Sfx.Click);
            panel.Open();
        }

        // ── IHoverLabel ──────────────────────────────────────────────────────────

        public string HoverLabel
        {
            get
            {
                if (panel == null || panel.IsOpen || !IsUnlocked) return null;

                int unread = NotificationService.Instance != null ? NotificationService.Instance.UnreadCount : 0;
                if (unread == 0) return hoverLabelText;

                // Read every hovered frame; rebuild only when the count changes.
                if (unread != _labelUnread)
                {
                    _labelUnread = unread;
                    _label       = $"{hoverLabelText} ({unread} new)";
                }
                return _label;
            }
        }

        private int    _labelUnread;
        private string _label;
    }
}
