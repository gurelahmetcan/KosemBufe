using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    /// <summary>
    /// One row of the phone order screen: an item, its live stock, +/- order amount, unit cost,
    /// and row subtotal. Reports changes back to PhoneOrderScreen so it can update the total.
    /// </summary>
    public class PhoneOrderRowUI : MonoBehaviour
    {
        [Header("References")]
        public Image           icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI stockText;
        public TextMeshProUGUI unitCostText;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI subtotalText;
        public Button          plusButton;
        public Button          minusButton;

        public IRestockable Target { get; private set; }
        public int          Amount { get; private set; }
        public float        Subtotal => Target != null ? Amount * Target.UnitCost : 0f;

        private Action _onChanged;

        public void Setup(IRestockable target, int initialAmount, Action onChanged)
        {
            Target     = target;
            Amount     = Mathf.Max(0, initialAmount);
            _onChanged = onChanged;

            if (icon != null)
            {
                icon.sprite  = target.Icon;
                icon.enabled = target.Icon != null;
            }
            if (nameText     != null) nameText.text     = target.DisplayName;
            if (stockText    != null) stockText.text    = $"{target.StockCount} in stock";
            if (unitCostText != null) unitCostText.text = $"{target.UnitCost:F0}$";

            if (plusButton  != null) plusButton.onClick.AddListener(() => Change(+1));
            if (minusButton != null) minusButton.onClick.AddListener(() => Change(-1));

            Refresh();
        }

        private void Change(int delta)
        {
            Amount = Mathf.Max(0, Amount + delta);   // min 0, no upper cap
            Refresh();
            _onChanged?.Invoke();
        }

        private void Refresh()
        {
            if (amountText   != null) amountText.text   = Amount.ToString();
            if (subtotalText != null) subtotalText.text = $"{Subtotal:F0}$";
        }
    }
}
