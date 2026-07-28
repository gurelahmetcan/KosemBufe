using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe
{
    public class ExpenseRowUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI amountText;

        [Tooltip("GameObject with a Toggle component. Hidden for mandatory expenses.")]
        public GameObject toggleRoot;

        [Header("Colors")]
        public Color activeColor   = new Color(0.85f, 0.15f, 0.15f, 1f);
        public Color inactiveColor = Color.white;

        /// <summary>Whether this expense is currently set to be deducted.</summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>Fired when the player changes the toggle. Passes the new enabled state.</summary>
        public event Action<bool> OnToggled;

        private float _amount;

        // ── Setup ─────────────────────────────────────────────────────────────────

        /// <summary>Initialize the row. Call once after Instantiate.</summary>
        public void Setup(string label, float amount, bool isMandatory)
        {
            _amount   = amount;
            IsEnabled = true;

            if (labelText != null) labelText.text = label;

            if (toggleRoot != null)
            {
                toggleRoot.SetActive(!isMandatory);

                var toggle = toggleRoot.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.isOn = true;
                    toggle.onValueChanged.AddListener(OnToggleValueChanged);
                }
            }

            Refresh();
        }

        /// <summary>Updates the displayed amount after setup (e.g. a restock row changing).</summary>
        public void SetAmount(float amount)
        {
            _amount = amount;
            Refresh();
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnToggleValueChanged(bool value)
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            IsEnabled = value;
            Refresh();
            OnToggled?.Invoke(IsEnabled);
        }

        private void Refresh()
        {
            if (amountText == null) return;

            Color c = IsEnabled ? activeColor : inactiveColor;

            amountText.text  = IsEnabled ? $"-{_amount:F0}" : "0";
            amountText.color = c;

            if (labelText != null)
                labelText.color = c;
        }
    }
}
