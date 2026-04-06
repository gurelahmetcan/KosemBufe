using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a Game Over screen and pauses the game when reputation hits 0
/// or the player goes bankrupt (money reaches 0).
/// Wire the ReputationManager and/or MoneyManager references; the panel starts hidden.
///
/// Scene Setup:
///   Canvas (Screen Space – Overlay, high sort order)
///     └─ GameOverPanel (starts inactive)
///         └─ "Game Over" text, optional restart button, etc.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    public ReputationManager reputationManager;

    [Tooltip("Money manager — triggers game over on bankruptcy.")]
    public MoneyManager moneyManager;

    [Tooltip("The panel to show on game over.")]
    public GameObject gameOverPanel;

    [Tooltip("Optional restart button.")]
    public Button restartButton;

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (reputationManager != null)
            reputationManager.OnGameOver += ShowGameOver;

        if (moneyManager != null)
            moneyManager.OnBankrupt += ShowGameOver;

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    private void OnDisable()
    {
        if (reputationManager != null)
            reputationManager.OnGameOver -= ShowGameOver;

        if (moneyManager != null)
            moneyManager.OnBankrupt -= ShowGameOver;

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);
    }

    private void ShowGameOver()
    {
        Debug.Log("[GameOverUI] Showing Game Over screen.");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
