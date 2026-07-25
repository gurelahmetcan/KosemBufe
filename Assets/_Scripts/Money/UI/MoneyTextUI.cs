using UnityEngine;
using TMPro;

namespace Bufe.UI
{
    /// <summary>
    /// Reusable money readout. Drop one on any screen that needs to show the balance (event
    /// screen, order panel, HUD…). It subscribes to MoneyManager and keeps its own TMP text in
    /// sync. Make it a prefab so every copy looks identical — each instance updates itself, so
    /// there is no shared UI object to fight over.
    /// </summary>
    public class MoneyTextUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Left empty, it finds the MoneyManager in the scene on enable.")]
        public MoneyManager    moneyManager;
        public TextMeshProUGUI text;

        [Header("Format")]
        [Tooltip("{0} = balance. e.g. \"₺{0:F0}\"")]
        public string format = "₺{0:F0}";

        private void Reset()
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (moneyManager == null) moneyManager = FindFirstObjectByType<MoneyManager>();
            if (moneyManager == null) return;

            moneyManager.OnBalanceChanged += HandleBalanceChanged;
            Render(moneyManager.Balance);   // refresh immediately so it's correct the moment it shows
        }

        private void OnDisable()
        {
            if (moneyManager != null)
                moneyManager.OnBalanceChanged -= HandleBalanceChanged;
        }

        private void HandleBalanceChanged(float balance, float delta) => Render(balance);

        private void Render(float balance)
        {
            if (text != null) text.text = string.Format(format, balance);
        }
    }
}
