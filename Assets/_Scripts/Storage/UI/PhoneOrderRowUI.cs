using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    public class PhoneOrderRowUI : MonoBehaviour
    {
        #region Public Fields
        
        [Header("References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI stockText;
        public TextMeshProUGUI unitCostText;
        public TextMeshProUGUI amountText;
        public Button          plusButton;
        public Button          minusButton;

        #endregion

        #region Variables

        public IRestockable Target { get; private set; }
        public int          Amount { get; private set; }
        public float        Subtotal => Target != null ? Amount * Target.UnitCost : 0f;

        private Action _onChanged;

        #endregion

        #region Public Methods

        public void Setup(IRestockable target, int initialAmount, Action onChanged)
        {
            Target     = target;
            Amount     = Mathf.Max(0, initialAmount);
            _onChanged = onChanged;
            
            if (nameText     != null) nameText.text     = target.DisplayName;
            if (stockText    != null) stockText.text    = $"{target.StockCount} in stock";
            if (unitCostText != null) unitCostText.text = $"₺{target.UnitCost:F0}";

            if (plusButton  != null) plusButton.onClick.AddListener(() => Change(+1));
            if (minusButton != null) minusButton.onClick.AddListener(() => Change(-1));

            Refresh();
        }

        #endregion

        #region Private Methods

        private void Change(int delta)
        {
            Amount = Mathf.Max(0, Amount + delta);   // min 0, no upper cap
            Refresh();
            _onChanged?.Invoke();
        }

        private void Refresh()
        {
            if (amountText   != null) amountText.text   = Amount.ToString();
        }

        #endregion
    }
}
