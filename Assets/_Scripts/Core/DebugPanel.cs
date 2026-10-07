using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime debug panel toggled with F1. Provides quick buttons for
/// testing game systems without going through normal gameplay.
///
/// Scene Setup:
///   1. Create a Canvas (Screen Space – Overlay).
///   2. Add a panel with buttons/labels as children.
///   3. Assign all references in the inspector.
///   4. The panel starts hidden; press F1 to toggle.
/// </summary>
namespace Bufe
{
    public class DebugPanel : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("Panel root (F1 toggles).")]
        public GameObject panel;

        [Header("Manager References")]
        public GameManager gameManager;
        public DayManager dayManager;
        public OrderManager orderManager;
        public MoneyManager moneyManager;

        [Header("Day Controls")]
        public Button endDayButton;

        [Header("Money Controls")]
        public Button addMoneyButton;
        public Button removeMoneyButton;
        [Tooltip("Amount added/removed per click.")]
        public float moneyStep = 100f;

        [Header("Time Speed Controls")]
        public Button speedUpButton;
        public Button speedDownButton;
        public Button normalSpeedButton;
        public TextMeshProUGUI speedLabel;

        [Header("Spawn Controls")]
        public Button toggleSpawnButton;
        public TextMeshProUGUI spawnStatusLabel;

        [Header("Status Display")]
        public TextMeshProUGUI statusText;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private float _baseSpeed;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                TogglePanel();

            if (panel != null && panel.activeSelf)
                RefreshStatus();
        }

        // ── Panel toggle ─────────────────────────────────────────────────────────

        private void TogglePanel()
        {
            if (panel == null) return;

            bool show = !panel.activeSelf;
            panel.SetActive(show);

            // Capture base speed on first open
            if (show && dayManager != null && dayManager.clock != null)
                _baseSpeed = dayManager.clock.GameMinutesPerRealSecond;
        }

        // ── Button wiring ────────────────────────────────────────────────────────

        private void WireButtons()
        {
            if (endDayButton != null)       endDayButton.onClick.AddListener(OnEndDay);
            if (addMoneyButton != null)     addMoneyButton.onClick.AddListener(OnAddMoney);
            if (removeMoneyButton != null)  removeMoneyButton.onClick.AddListener(OnRemoveMoney);
            if (speedUpButton != null)      speedUpButton.onClick.AddListener(OnSpeedUp);
            if (speedDownButton != null)    speedDownButton.onClick.AddListener(OnSpeedDown);
            if (normalSpeedButton != null)  normalSpeedButton.onClick.AddListener(OnNormalSpeed);
            if (toggleSpawnButton != null)  toggleSpawnButton.onClick.AddListener(OnToggleSpawn);
        }

        private void UnwireButtons()
        {
            if (endDayButton != null)       endDayButton.onClick.RemoveListener(OnEndDay);
            if (addMoneyButton != null)     addMoneyButton.onClick.RemoveListener(OnAddMoney);
            if (removeMoneyButton != null)  removeMoneyButton.onClick.RemoveListener(OnRemoveMoney);
            if (speedUpButton != null)      speedUpButton.onClick.RemoveListener(OnSpeedUp);
            if (speedDownButton != null)    speedDownButton.onClick.RemoveListener(OnSpeedDown);
            if (normalSpeedButton != null)  normalSpeedButton.onClick.RemoveListener(OnNormalSpeed);
            if (toggleSpawnButton != null)  toggleSpawnButton.onClick.RemoveListener(OnToggleSpawn);
        }

        // ── Button handlers ──────────────────────────────────────────────────────

        private void OnEndDay()
        {
            if (dayManager != null)
            {
                dayManager.ForceEndDay();
                Log.Info("[Debug] Forced day end.");
            }
        }

        private void OnAddMoney()
        {
            if (moneyManager != null)
            {
                moneyManager.AddIncome(moneyStep);
                Log.Info($"[Debug] Added ${moneyStep}.");
            }
        }

        private void OnRemoveMoney()
        {
            if (moneyManager != null)
            {
                moneyManager.DeductExpense(moneyStep, "debug");
                Log.Info($"[Debug] Removed ${moneyStep}.");
            }
        }

        private void OnSpeedUp()
        {
            if (dayManager == null || dayManager.clock == null) return;

            float current = dayManager.clock.GameMinutesPerRealSecond;
            dayManager.clock.SetSpeed(current * 2f);
            RefreshSpeedLabel();
        }

        private void OnSpeedDown()
        {
            if (dayManager == null || dayManager.clock == null) return;

            float current = dayManager.clock.GameMinutesPerRealSecond;
            dayManager.clock.SetSpeed(Mathf.Max(0.5f, current * 0.5f));
            RefreshSpeedLabel();
        }

        private void OnNormalSpeed()
        {
            if (dayManager == null || dayManager.clock == null) return;

            if (_baseSpeed <= 0f) _baseSpeed = 2f; // sensible fallback
            dayManager.clock.SetSpeed(_baseSpeed);
            RefreshSpeedLabel();
        }

        private void OnToggleSpawn()
        {
            if (orderManager == null) return;

            orderManager.SetSpawningPaused(!orderManager.SpawningPaused);
            RefreshSpawnLabel();
        }

        // ── Status display ───────────────────────────────────────────────────────

        private void RefreshStatus()
        {
            RefreshSpeedLabel();
            RefreshSpawnLabel();

            if (statusText == null) return;

            string day   = dayManager != null ? $"Day {dayManager.CurrentDay}" : "?";
            string phase = dayManager != null ? dayManager.CurrentPhase.ToString() : "?";
            string money = moneyManager != null ? $"${moneyManager.Balance:F0}" : "?";
            string time  = dayManager != null && dayManager.clock != null
                ? $"{dayManager.clock.CurrentHour:D2}:{dayManager.clock.CurrentMinute:D2}"
                : "?";

            statusText.text = $"{day} | {phase} | {time} | Money: {money}";
        }

        private void RefreshSpeedLabel()
        {
            if (speedLabel == null || dayManager == null || dayManager.clock == null) return;
            float speed = dayManager.clock.GameMinutesPerRealSecond;
            float multiplier = _baseSpeed > 0f ? speed / _baseSpeed : 1f;
            speedLabel.text = $"Speed: {multiplier:F1}x";
        }

        private void RefreshSpawnLabel()
        {
            if (spawnStatusLabel == null || orderManager == null) return;
            spawnStatusLabel.text = orderManager.SpawningPaused ? "Spawning: OFF" : "Spawning: ON";
        }
    }

}
