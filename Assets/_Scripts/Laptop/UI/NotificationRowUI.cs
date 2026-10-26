using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    /// <summary>One line in the laptop's notification dropdown.</summary>
    public class NotificationRowUI : MonoBehaviour
    {
        public TextMeshProUGUI messageText;
        public TextMeshProUGUI dayText;

        [Tooltip("Optional stripe tinted by kind.")]
        public Graphic kindMarker;

        [Tooltip("Optional unread dot.")]
        public GameObject unreadDot;

        public Color infoColor    = new Color(0.55f, 0.55f, 0.55f, 1f);
        public Color successColor = new Color(0.30f, 0.70f, 0.35f, 1f);
        public Color warningColor = new Color(0.90f, 0.45f, 0.20f, 1f);

        public void Set(Notification n)
        {
            if (messageText != null) messageText.text = n.message;
            if (dayText     != null) dayText.text     = $"Day {n.day}";
            if (unreadDot   != null) unreadDot.SetActive(!n.isRead);

            if (kindMarker != null)
            {
                kindMarker.color = n.kind switch
                {
                    NotificationKind.Success => successColor,
                    NotificationKind.Warning => warningColor,
                    _                        => infoColor,
                };
            }
        }
    }
}
