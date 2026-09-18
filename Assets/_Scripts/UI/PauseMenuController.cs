using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bufe.UI
{
    /// <summary>
    /// ESC-driven pause menu. Freezes the game, frees the cursor, and offers Resume,
    /// a look-sensitivity slider, and a Return to Menu button.
    ///
    /// Setup:
    ///   1. Attach to a manager GameObject in the GameScene.
    ///   2. Assign 'pauseRoot' to the pause panel (starts hidden automatically).
    ///   3. Assign 'playerLook' to the Player's PlayerLook component.
    ///   4. Wire the Resume / Main Menu buttons and the sensitivity Slider.
    ///   5. Set the Slider's Min/Max Value (e.g. 0.02 – 0.5) in the Inspector.
    ///
    /// Note: ESC is owned by this controller — PlayerLook no longer intercepts it.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Root panel shown while paused. Hidden on Awake.")]
        [SerializeField] private GameObject pauseRoot;

        [Tooltip("The player's PlayerLook — used to free the cursor and apply sensitivity.")]
        [SerializeField] private PlayerLook playerLook;

        [Header("Sensitivity")]
        [Tooltip("Slider controlling look sensitivity. Set its Min/Max in the Inspector.")]
        [SerializeField] private Slider sensitivitySlider;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Exit")]
        [Tooltip("Scene loaded by the Return to Menu button.")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        public bool IsPaused { get; private set; }

        // UIMode to restore when unpausing — captured before we force it true. Safety net so
        // that if the menu ever opens while another UI owns the cursor, resuming puts the
        // cursor state back the way it was instead of forcing gameplay (crosshair on).
        private bool _resumeUIMode;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            // Fall back to the scene's PlayerLook if the reference wasn't wired — the
            // UIMode guard below depends on it, and a null reference would silently
            // disable the guard (letting ESC pause over dialogue/event screens).
            if (playerLook == null) playerLook = FindObjectOfType<PlayerLook>();

            if (pauseRoot != null) pauseRoot.SetActive(false);

            if (resumeButton   != null) resumeButton.onClick.AddListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        private void OnDestroy()
        {
            if (resumeButton   != null) resumeButton.onClick.RemoveListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Toggle();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void Toggle()
        {
            if (IsPaused) Resume();
            else          Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;

            // Only pause during normal play — never over the game-over / demo-end screens.
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // Don't open on top of another modal UI (intro/dialogue, night events, day-end,
            // recipe book, order prompt). Those already own the cursor via UIMode — pausing
            // over them and then resuming would wrongly force the cursor back to gameplay.
            if (playerLook != null && playerLook.UIMode) return;

            IsPaused = true;

            // Remember the current cursor ownership so Resume() restores it exactly.
            _resumeUIMode = playerLook != null && playerLook.UIMode;

            // Reflect the current (possibly saved) sensitivity without firing the callback.
            if (sensitivitySlider != null && playerLook != null)
                sensitivitySlider.SetValueWithoutNotify(playerLook.sensitivity);

            if (pauseRoot != null) pauseRoot.SetActive(true);
            if (playerLook != null) playerLook.UIMode = true;   // frees the cursor, suppresses look

            if (GameManager.Instance != null) GameManager.Instance.PauseGame();
            else                              Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (!IsPaused) return;

            IsPaused = false;

            if (pauseRoot != null) pauseRoot.SetActive(false);

            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
            else                              Time.timeScale = 1f;

            // Restore whatever cursor state we had before pausing. In normal gameplay this is
            // false (re-locks cursor, crosshair back). If the menu somehow opened over another
            // UI, this leaves that UI's free cursor intact — no stray crosshair.
            if (playerLook != null) playerLook.UIMode = _resumeUIMode;
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;                                // clear the pause freeze before loading

            // The cursor must stay free for the menu scene. Cursor state is global and
            // survives the scene load, so setting UIMode = false here would re-lock and hide
            // it (LockCursor) and the main menu would be unclickable.
            IsPaused = false;
            if (pauseRoot != null) pauseRoot.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            if (AudioManager.Instance) AudioManager.Instance.PlaySfx(Sfx.Click);

            SceneManager.LoadScene(mainMenuScene);
        }

        // ── Callbacks ──────────────────────────────────────────────────────────────

        private void OnSensitivityChanged(float value)
        {
            if (playerLook != null) playerLook.SetSensitivity(value);
        }
    }
}
