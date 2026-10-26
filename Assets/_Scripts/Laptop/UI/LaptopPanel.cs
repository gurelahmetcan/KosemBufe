using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    /// <summary>
    /// The büfe laptop's management panel: top bar (day, time, balance, notifications, close),
    /// side menu and a content area that hosts one LaptopSection at a time. Opened by BufeLaptop.
    /// Back (Escape / right-click) closes the topmost layer: modal → notifications → panel.
    /// </summary>
    public class LaptopPanel : MonoBehaviour
    {
        /// <summary>A side-menu button. Leave <see cref="section"/> empty for pages not built yet - the button is disabled.</summary>
        [Serializable]
        public class MenuEntry
        {
            public LaptopSection   section;
            public Button          button;
            public TextMeshProUGUI label;
        }

        [Header("References")]
        public DayManager   dayManager;
        public DayClock     clock;
        public MoneyManager moneyManager;
        public PlayerLook   playerLook;

        [Header("Root")]
        public GameObject panelRoot;

        [Header("Top Bar")]
        public TextMeshProUGUI dayText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI balanceText;
        public Button          closeButton;

        [Header("Notifications")]
        public Button          bellButton;
        public GameObject      unreadBadge;
        public TextMeshProUGUI unreadCountText;
        public GameObject      notificationDropdown;
        public Transform       notificationContainer;
        public GameObject      notificationRowPrefab;   // must have a NotificationRowUI
        public GameObject      emptyNotificationsLabel;

        [Header("Side Menu")]
        public MenuEntry[] menu;
        [Tooltip("Menu index opened first.")]
        public int   defaultMenuIndex = 0;
        public Color activeMenuColor   = Color.black;
        public Color inactiveMenuColor = Color.gray;

        [Header("Overlays")]
        public LaptopModal modal;
        public LaptopToast toast;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private LaptopSection       _current;
        private NotificationService _notifications;
        private bool                _wired;
        private readonly List<GameObject> _notificationRows = new List<GameObject>();

        public bool IsOpen { get; private set; }

        public MoneyManager Money => moneyManager;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (dayManager   == null) dayManager   = FindAnyObjectByType<DayManager>();
            if (clock        == null) clock        = FindAnyObjectByType<DayClock>();
            if (moneyManager == null) moneyManager = FindAnyObjectByType<MoneyManager>();
            if (playerLook   == null) playerLook   = FindAnyObjectByType<PlayerLook>();
        }

        private void Start()
        {
            EnsureWired();

            // Start can run right after the first Open() when this lives on an inactive
            // object - don't hide a panel the player just opened.
            if (!IsOpen && panelRoot != null) panelRoot.SetActive(false);

            // Subscribed here, not in OnEnable: the service sets Instance in its own Awake,
            // which may run after our OnEnable.
            _notifications = NotificationService.Instance;
            if (_notifications != null) _notifications.OnChanged += RefreshNotifications;
        }

        private void OnDestroy()
        {
            if (_notifications != null) _notifications.OnChanged -= RefreshNotifications;
        }

        private void OnEnable()
        {
            if (clock        != null) clock.OnTimeChanged           += HandleTimeChanged;
            if (moneyManager != null) moneyManager.OnBalanceChanged += HandleBalanceChanged;
        }

        private void OnDisable()
        {
            if (clock        != null) clock.OnTimeChanged           -= HandleTimeChanged;
            if (moneyManager != null) moneyManager.OnBalanceChanged -= HandleBalanceChanged;
        }

        /// <summary>
        /// LateUpdate, not Update: PauseMenuController toggles on Escape in its Update and skips
        /// while UIMode is on. Closing here - after every Update - keeps the same Escape press
        /// from also opening the pause menu.
        /// </summary>
        private void LateUpdate()
        {
            if (!IsOpen) return;

            Keyboard kb    = Keyboard.current;
            Mouse    mouse = Mouse.current;

            bool back = (kb    != null && kb.escapeKey.wasPressedThisFrame)
                     || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            if (!back) return;

            if (modal != null && modal.IsOpen)                                   modal.Dismiss();
            else if (notificationDropdown != null && notificationDropdown.activeSelf) SetNotificationsOpen(false);
            else                                                                 Close();
        }

        /// <summary>
        /// Wires buttons and binds sections once. Called from Start and Open, so it works even
        /// when the panel lives on a canvas that starts inactive.
        /// </summary>
        private void EnsureWired()
        {
            if (_wired) return;
            _wired = true;

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (bellButton  != null) bellButton.onClick.AddListener(ToggleNotifications);

            if (menu == null) return;
            foreach (var entry in menu)
            {
                if (entry == null) continue;

                if (entry.section != null)
                {
                    entry.section.Bind(this);
                    entry.section.Hide();
                }

                if (entry.button == null) continue;
                entry.button.interactable = entry.section != null;

                LaptopSection target = entry.section;   // local copy for the closure
                entry.button.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance) AudioManager.Instance.PlaySfx(Sfx.Click);
                    ShowSection(target);
                });
            }
        }

        // ── Open / close ─────────────────────────────────────────────────────────

        public void Open()
        {
            if (IsOpen) return;
            EnsureWired();

            IsOpen = true;
            if (panelRoot  != null) panelRoot.SetActive(true);
            if (playerLook != null) playerLook.UIMode = true;

            SetNotificationsOpen(false);
            RefreshTopBar();
            RefreshNotifications();

            ShowSection(GetDefaultSection());
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;

            if (modal != null && modal.IsOpen) modal.Dismiss();
            if (_current != null) { _current.Hide(); _current = null; }

            if (AudioManager.Instance) AudioManager.Instance.PlaySfx(Sfx.Click);

            if (panelRoot  != null) panelRoot.SetActive(false);
            if (playerLook != null) playerLook.UIMode = false;
        }

        // ── Sections ─────────────────────────────────────────────────────────────

        public void ShowSection(LaptopSection section)
        {
            if (section == null) return;

            if (_current != null && _current != section) _current.Hide();
            _current = section;
            _current.Show();

            SetNotificationsOpen(false);
            RefreshMenuHighlight();
        }

        /// <summary>Opens the first section of type T (used by cross-links like "Reorder → Restock").</summary>
        public T ShowSection<T>() where T : LaptopSection
        {
            if (menu == null) return null;
            foreach (var entry in menu)
            {
                if (entry?.section is T match)
                {
                    ShowSection(match);
                    return match;
                }
            }
            return null;
        }

        private LaptopSection GetDefaultSection()
        {
            if (menu == null || menu.Length == 0) return null;

            if (defaultMenuIndex >= 0 && defaultMenuIndex < menu.Length && menu[defaultMenuIndex]?.section != null)
                return menu[defaultMenuIndex].section;

            foreach (var entry in menu)
                if (entry?.section != null) return entry.section;
            return null;
        }

        private void RefreshMenuHighlight()
        {
            if (menu == null) return;
            foreach (var entry in menu)
            {
                if (entry?.label == null) continue;
                entry.label.color = entry.section != null && entry.section == _current
                    ? activeMenuColor
                    : inactiveMenuColor;
            }
        }

        // ── Overlays (used by sections) ──────────────────────────────────────────

        public void ShowToast(string message)
        {
            if (toast != null) toast.Show(message);
        }

        public void ShowModal(string title, string message, string confirmText, Action onConfirm,
                              string cancelText = null, Action onCancel = null)
        {
            if (modal != null)
                modal.Show(title, message, confirmText, onConfirm, cancelText, onCancel);
            else
                onConfirm?.Invoke();   // no modal wired - don't block the flow
        }

        // ── Top bar ──────────────────────────────────────────────────────────────

        private void RefreshTopBar()
        {
            if (dayText != null && dayManager != null) dayText.text = $"Day {dayManager.CurrentDay}";
            if (clock != null) HandleTimeChanged(clock.CurrentHour, clock.CurrentMinute);
            if (moneyManager != null) HandleBalanceChanged(moneyManager.Balance, 0f);
        }

        private void HandleTimeChanged(int hour, int minute)
        {
            if (!IsOpen || timeText == null) return;
            timeText.text = $"{hour:D2}:{minute:D2}";
        }

        private void HandleBalanceChanged(float balance, float delta)
        {
            if (!IsOpen || balanceText == null) return;
            balanceText.text = $"₺{balance:F0}";
        }

        // ── Notifications ────────────────────────────────────────────────────────

        private void ToggleNotifications()
        {
            if (AudioManager.Instance) AudioManager.Instance.PlaySfx(Sfx.Click);

            bool open = notificationDropdown != null && !notificationDropdown.activeSelf;
            SetNotificationsOpen(open);
        }

        private void SetNotificationsOpen(bool open)
        {
            if (notificationDropdown == null) return;
            notificationDropdown.SetActive(open);

            if (!open) return;

            // Build the list first so unread dots show, then clear the badge.
            RebuildNotificationRows();
            NotificationService.Instance?.MarkAllRead();
        }

        private void RefreshNotifications()
        {
            var service = NotificationService.Instance;
            int unread  = service != null ? service.UnreadCount : 0;

            if (unreadBadge     != null) unreadBadge.SetActive(unread > 0);
            if (unreadCountText != null) unreadCountText.text = unread > 9 ? "9+" : unread.ToString();
        }

        private void RebuildNotificationRows()
        {
            foreach (var go in _notificationRows)
                if (go != null) Destroy(go);
            _notificationRows.Clear();

            var service = NotificationService.Instance;
            bool empty  = service == null || service.Items.Count == 0;
            if (emptyNotificationsLabel != null) emptyNotificationsLabel.SetActive(empty);

            if (empty || notificationContainer == null || notificationRowPrefab == null) return;

            foreach (var n in service.Items)
            {
                var go  = Instantiate(notificationRowPrefab, notificationContainer);
                var row = go.GetComponent<NotificationRowUI>();
                if (row != null) row.Set(n);
                _notificationRows.Add(go);
            }
        }
    }
}
