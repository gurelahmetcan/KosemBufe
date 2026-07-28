using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    /// <summary>
    /// Day-end phone screen for reordering stock. Lists every finite storage with a +/- order
    /// amount and running total. The order is NOT committed here — Confirm reports the pending
    /// order to DayEndScreen, which applies stock + cost at Sleep (so it stays editable until then).
    /// </summary>
    public class PhoneOrderScreen : MonoBehaviour
    {
        [Header("Storages")]
        public IngredientStorage[] ingredientStorages;
        public PremadeStorage[]    premadeStorages;

        [Header("UI")]
        public GameObject      screenRoot;
        public Transform       rowContainer;
        public GameObject      rowPrefab;       // must have a PhoneOrderRowUI
        public TextMeshProUGUI totalText;
        public Button          confirmButton;
        public Button          cancelButton;

        [Header("Cart")]
        public Transform       cartContainer;   // where cart lines are listed
        public GameObject      cartRowPrefab;   // must have a CartRowUI

        [Header("Category Tabs")]
        public CategoryTab[]   tabs;
        public Color           activeTabColor   = Color.black;
        public Color           inactiveTabColor = Color.gray;

        /// <summary>A top tab that filters the item list to one category.</summary>
        [Serializable]
        public class CategoryTab
        {
            public OrderCategory   category;
            public Button          button;
            public TextMeshProUGUI label;
        }

        /// <summary>Player confirmed an order. Passes the pending lines and total cost.</summary>
        public event Action<List<RestockLine>, float> OnOrderConfirmed;

        /// <summary>Player cancelled — no changes.</summary>
        public event Action OnCancelled;

        private readonly List<PhoneOrderRowUI> _rows     = new List<PhoneOrderRowUI>();
        private readonly List<CartRowUI>       _cartRows = new List<CartRowUI>();

        private bool _wired;

        private void Start()
        {
            EnsureWired();
            if (screenRoot != null) screenRoot.SetActive(false);
        }

        /// <summary>
        /// Wires button listeners once. Called from Start and again from Open, so the buttons work
        /// even when this screen lives on a canvas that starts inactive (Start would never run).
        /// </summary>
        private void EnsureWired()
        {
            if (_wired) return;
            _wired = true;

            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (cancelButton  != null) cancelButton.onClick.AddListener(HandleCancel);

            if (tabs != null)
            {
                foreach (var tab in tabs)
                {
                    if (tab?.button == null) continue;
                    OrderCategory cat = tab.category;          // local copy for the closure
                    tab.button.onClick.AddListener(() => SelectCategory(cat));
                }
            }
        }

        /// <summary>Opens the screen, pre-filling order amounts from the current pending order.</summary>
        public void Open(IReadOnlyList<RestockLine> pending)
        {
            EnsureWired();
            BuildRows(pending);
            HandleRowChanged();
            SelectCategory(tabs != null && tabs.Length > 0 ? tabs[0].category : OrderCategory.StapleFood);
            if (screenRoot != null) screenRoot.SetActive(true);
        }

        public void Close()
        {
            if (screenRoot != null) screenRoot.SetActive(false);
        }

        // ── Rows ───────────────────────────────────────────────────────────────────

        private void BuildRows(IReadOnlyList<RestockLine> pending)
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();

            if (rowContainer == null || rowPrefab == null) return;

            foreach (IRestockable storage in EnumerateRestockables())
            {
                if (storage == null || storage.IsInfinite) continue;

                var go  = Instantiate(rowPrefab, rowContainer);
                var row = go.GetComponent<PhoneOrderRowUI>();
                if (row == null) { Destroy(go); continue; }

                row.Setup(storage, LookupPending(pending, storage), HandleRowChanged);
                _rows.Add(row);
            }
        }

        private IEnumerable<IRestockable> EnumerateRestockables()
        {
            if (ingredientStorages != null)
                foreach (var s in ingredientStorages) yield return s;
            if (premadeStorages != null)
                foreach (var s in premadeStorages) yield return s;
        }

        private static int LookupPending(IReadOnlyList<RestockLine> pending, IRestockable target)
        {
            if (pending == null) return 0;
            foreach (var line in pending)
                if (ReferenceEquals(line.target, target)) return line.amount;
            return 0;
        }

        private void HandleRowChanged()
        {
            UpdateTotal();
            RefreshCart();
        }

        private void UpdateTotal()
        {
            float total = 0f;
            foreach (var row in _rows) total += row.Subtotal;
            if (totalText != null) totalText.text = $"₺{total:F0}";
        }

        // ── Cart ───────────────────────────────────────────────────────────────────

        private void RefreshCart()
        {
            foreach (var cart in _cartRows)
                if (cart != null) Destroy(cart.gameObject);
            _cartRows.Clear();

            if (cartContainer == null || cartRowPrefab == null) return;

            foreach (var row in _rows)
            {
                if (row.Amount <= 0) continue;

                var go   = Instantiate(cartRowPrefab, cartContainer);
                var cart = go.GetComponent<CartRowUI>();
                if (cart == null) { Destroy(go); continue; }

                cart.Set($"{row.Target.DisplayName} x{row.Amount}", $"₺{row.Subtotal:F0}");
                _cartRows.Add(cart);
            }
        }

        // ── Category tabs ────────────────────────────────────────────────────────────

        /// <summary>Shows only rows in the given category and highlights the matching tab.</summary>
        private void SelectCategory(OrderCategory category)
        {
            foreach (var row in _rows)
            {
                if (row == null || row.Target == null) continue;
                row.gameObject.SetActive(row.Target.Category == category);
            }

            if (tabs == null) return;
            foreach (var tab in tabs)
            {
                if (tab?.label == null) continue;
                tab.label.color = tab.category == category ? activeTabColor : inactiveTabColor;
            }
        }

        // ── Buttons ────────────────────────────────────────────────────────────────

        private void HandleConfirm()
        {
            var lines = new List<RestockLine>();
            float total = 0f;

            foreach (var row in _rows)
            {
                if (row.Amount <= 0) continue;
                lines.Add(new RestockLine(row.Target, row.Amount));
                total += row.Subtotal;
            }

            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            Close();
            OnOrderConfirmed?.Invoke(lines, total);
        }

        private void HandleCancel()
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            Close();
            OnCancelled?.Invoke();
        }
    }
}
