using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public enum NotificationKind { Info, Success, Warning }

    /// <summary>A single entry in the laptop's notification center.</summary>
    public class Notification
    {
        public readonly string           message;
        public readonly NotificationKind kind;
        public readonly int              day;
        public bool                      isRead;

        public Notification(string message, NotificationKind kind, int day)
        {
            this.message = message;
            this.kind    = kind;
            this.day     = day;
        }
    }

    /// <summary>
    /// Collects notifications from game systems (order delivered, stock low, …) for the laptop's
    /// bell / notification center. Any system can post via NotificationService.Instance.Add.
    /// </summary>
    public class NotificationService : MonoBehaviour
    {
        public static NotificationService Instance { get; private set; }

        [Header("References")]
        public DayManager dayManager;

        [Tooltip("Max kept notifications.")]
        [Min(1)] public int maxNotifications = 30;

        /// <summary>Fired whenever a notification is added or read state changes.</summary>
        public event Action OnChanged;

        private readonly List<Notification> _items = new List<Notification>();

        /// <summary>Newest first.</summary>
        public IReadOnlyList<Notification> Items => _items;

        public int UnreadCount
        {
            get
            {
                int count = 0;
                foreach (var n in _items)
                    if (!n.isRead) count++;
                return count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (dayManager == null) dayManager = FindAnyObjectByType<DayManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Add(string message, NotificationKind kind = NotificationKind.Info)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            int day = dayManager != null ? dayManager.CurrentDay : 0;
            _items.Insert(0, new Notification(message, kind, day));

            if (_items.Count > maxNotifications)
                _items.RemoveRange(maxNotifications, _items.Count - maxNotifications);

            OnChanged?.Invoke();
        }

        public void MarkAllRead()
        {
            bool changed = false;
            foreach (var n in _items)
            {
                if (n.isRead) continue;
                n.isRead = true;
                changed  = true;
            }

            if (changed) OnChanged?.Invoke();
        }
    }
}
