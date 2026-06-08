using UnityEngine;
using TMPro;

namespace Bufe.UI
{
    public class MoneyUI : MonoBehaviour
    {
        [Header("References")]
        public MoneyManager moneyManager;

        [Tooltip("Text showing the current balance.")]
        public TextMeshProUGUI balanceText;

        [Header("Format")]
        [Tooltip("Prefix shown before the amount (e.g. currency symbol).")]
        public string currencyPrefix = "₺";

        [Header("Colors")]
        public Color positiveColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        public Color negativeColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            if (moneyManager != null)
                moneyManager.OnBalanceChanged += OnBalanceChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (moneyManager != null)
                moneyManager.OnBalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(float current, float delta)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (moneyManager == null || balanceText == null) return;

            float balance = moneyManager.Balance;
            balanceText.text = $"{currencyPrefix} {balance:F2}";
            balanceText.color = balance >= 0f ? positiveColor : negativeColor;
        }
    }
}