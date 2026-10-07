using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Game over panel.")]
        public GameObject gameOverPanel;

        [Tooltip("Reason text. Optional.")]
        public TextMeshProUGUI reasonText;

        [Tooltip("Restart button. Optional.")]
        public Button restartButton;

        private void Awake()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        /// <summary>
        /// Called by GameManager when a game-over condition is met.
        /// </summary>
        public void ShowGameOver(string reason)
        {
            Log.Info($"[GameOverUI] Showing Game Over: {reason}");

            if (reasonText != null)
                reasonText.text = reason;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RestartGame();
        }
    }
}
