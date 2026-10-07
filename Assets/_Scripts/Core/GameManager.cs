using System;
using Bufe.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bufe
{
    public class GameManager : MonoBehaviour
    {
        public enum GameState { Playing, Paused, GameOver }

        // ── Singleton ────────────────────────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        // ── Configuration ────────────────────────────────────────────────────────

        [Header("Configuration")]
        [Tooltip("Game balance config.")]
        public GameConfig config;

        // ── Manager references ───────────────────────────────────────────────────

        [Header("Managers")]
        public DayManager dayManager;
        public OrderManager orderManager;
        public MoneyManager moneyManager;

        [Header("UI")]
        public GameOverUI gameOverUI;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when the game state changes (Playing, Paused, GameOver).</summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>Fired when a day ends and all expenses are settled. Passes day number.</summary>
        public event Action<int> OnDaySettled;

        // ── Runtime ──────────────────────────────────────────────────────────────

        public GameState CurrentState { get; private set; } = GameState.Playing;

        /// <summary>True when the game is in a playable state (not paused or game over).</summary>
        public bool IsPlaying => CurrentState == GameState.Playing;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ValidateReferences();
            ApplyConfig();
        }

        private void OnEnable()
        {
            if (moneyManager != null)
                moneyManager.OnBankrupt += OnBankrupt;

            if (dayManager != null)
                dayManager.OnDayEnded += OnDayEnded;
        }

        private void OnDisable()
        {
            if (moneyManager != null)
                moneyManager.OnBankrupt -= OnBankrupt;

            if (dayManager != null)
                dayManager.OnDayEnded -= OnDayEnded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.Paused);
            Time.timeScale = 0f;
            Log.Info("[GameManager] Game paused.");
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            SetState(GameState.Playing);
            Time.timeScale = 1f;
            Log.Info("[GameManager] Game resumed.");
        }

        public void TriggerGameOver(string reason)
        {
            if (CurrentState == GameState.GameOver) return;

            SetState(GameState.GameOver);
            Time.timeScale = 0f;

            Log.Info($"[GameManager] GAME OVER - {reason}");

            if (gameOverUI != null)
                gameOverUI.ShowGameOver(reason);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ── Config distribution ──────────────────────────────────────────────────

        private void ApplyConfig()
        {
            if (config == null)
            {
                Debug.LogWarning("[GameManager] GameConfig not assigned - using defaults.", this);
                return;
            }

            if (moneyManager != null)
                moneyManager.Init(config.startingMoney);

            if (orderManager != null)
                orderManager.Init(config.orderDuration, config.timeBetweenCustomers);

            if (dayManager != null)
                dayManager.demoLastDay = config.demoLastDay;
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void OnBankrupt()
        {
            TriggerGameOver("You went bankrupt!");
        }

        /// <summary>
        /// Notifies listeners that the day has ended.
        /// Expenses are deducted later - after the player clicks Sleep on the day-end screen.
        /// </summary>
        private void OnDayEnded(int dayNumber)
        {
            OnDaySettled?.Invoke(dayNumber);

            Log.Info($"[GameManager] Day {dayNumber} settled. Money: {moneyManager?.Balance:F2}");
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(CurrentState);
        }

        private void ValidateReferences()
        {
            if (config == null) Debug.LogWarning("[GameManager] GameConfig is not assigned!", this);
            if (dayManager == null) Debug.LogWarning("[GameManager] DayManager is not assigned!", this);
            if (orderManager == null) Debug.LogWarning("[GameManager] OrderManager is not assigned!", this);
            if (moneyManager == null) Debug.LogWarning("[GameManager] MoneyManager is not assigned!", this);
            if (gameOverUI == null) Debug.LogWarning("[GameManager] GameOverUI is not assigned!", this);
        }
    }

}
