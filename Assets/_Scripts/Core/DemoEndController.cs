using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Bufe.UI;

namespace Bufe
{
    /// <summary>
    /// Shows the demo-end screen when the final demo day ends (DayManager.OnDemoComplete),
    /// recapping the player's logged event decisions (DecisionLog). Wire the Main Menu / Quit
    /// buttons to the public methods.
    /// </summary>
    public class DemoEndController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DayManager      dayManager;
        [SerializeField] private GameObject      screenRoot;
        [SerializeField] private TextMeshProUGUI decisionsText;
        [SerializeField] private PlayerLook      playerLook;
        [SerializeField] private ScreenFader     screenFader;

        [Header("Rent Goal")]
        [Tooltip("Compared against GameConfig.rentTarget.")]
        [SerializeField] private MoneyManager moneyManager;

        [Tooltip("Rent goal result. Optional.")]
        [SerializeField] private TextMeshProUGUI rentResultText;

        [Tooltip("Goal met. {0} = balance, {1} = rent.")]
        [TextArea(1, 3)]
        [SerializeField] private string rentSuccessText = "You saved ₺{0} - enough to cover the ₺{1} rent.";

        [Tooltip("Goal missed. {0} = balance, {1} = rent.")]
        [TextArea(1, 3)]
        [SerializeField] private string rentFailText = "You saved ₺{0} - not enough for the ₺{1} rent.";

        [Header("Formatting")]
        [Tooltip("Prefix for each decision line.")]
        [SerializeField] private string bullet = "• ";
        [Tooltip("Shown when no decisions were logged.")]
        [SerializeField] private string emptyText = "...";

        [Header("Exit")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        private void Start()
        {
            if (screenRoot != null) screenRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (dayManager != null) dayManager.OnDemoComplete += ShowDemoEnd;
        }

        private void OnDisable()
        {
            if (dayManager != null) dayManager.OnDemoComplete -= ShowDemoEnd;
        }

        private void ShowDemoEnd() => StartCoroutine(ShowRoutine());

        private IEnumerator ShowRoutine()
        {
            if (screenFader != null) yield return screenFader.FadeOut();   // to black

            if (playerLook != null) playerLook.UIMode = true;
            PopulateDecisions();
            PopulateRentResult();
            if (screenRoot != null) screenRoot.SetActive(true);

            if (screenFader != null) yield return screenFader.FadeIn();    // reveal the screen
        }

        private void PopulateDecisions()
        {
            if (decisionsText == null) return;

            var entries = DecisionLog.Instance != null ? DecisionLog.Instance.Entries : null;
            if (entries == null || entries.Count == 0)
            {
                decisionsText.text = emptyText;
                return;
            }

            var sb = new StringBuilder();
            foreach (string line in entries)
                sb.Append(bullet).Append(line).Append('\n');

            decisionsText.text = sb.ToString().TrimEnd();
        }

        /// <summary>Compares the final balance against GameConfig.rentTarget and shows the verdict.</summary>
        private void PopulateRentResult()
        {
            if (rentResultText == null) return;

            float target  = GameManager.Instance != null && GameManager.Instance.config != null
                ? GameManager.Instance.config.rentTarget
                : 0f;
            float balance = moneyManager != null ? moneyManager.Balance : 0f;

            bool reached = balance >= target;
            string format = reached ? rentSuccessText : rentFailText;

            rentResultText.text = string.Format(format, balance.ToString("F0"), target.ToString("F0"));
        }

        // ── Button hooks ───────────────────────────────────────────────────────

        /// <summary>Wire to the demo-end "Main Menu" button.</summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }

        /// <summary>Wire to a "Quit" button.</summary>
        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
