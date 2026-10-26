using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    /// <summary>
    /// Laptop "Restock" page (getirsene.com.tr). Lists every finite storage with a +/- amount,
    /// a cart and a running total. The cart lives in SupplyService, so it survives closing the
    /// panel. Confirm pays immediately (after a confirmation modal) and the goods arrive the
    /// next morning.
    /// </summary>
    public class PhoneOrderScreen : LaptopSection
    {
        [Header("References")]
        public SupplyService supplyService;

        [Header("UI")]
        public Transform       rowContainer;
        public GameObject      rowPrefab;       // must have a PhoneOrderRowUI
        public TextMeshProUGUI totalText;
        public Button          confirmButton;

        [Tooltip("Empties the cart.")]
        [FormerlySerializedAs("cancelButton")]
        public Button          clearCartButton;

        [Header("Cart")]
        public Transform       cartContainer;   // where cart lines are listed
        public GameObject      cartRowPrefab;   // must have a CartRowUI

        [Header("In Transit")]
        [Tooltip("'Arriving tomorrow' block. Hidden if empty.")]
        public GameObject      inTransitRoot;
        public TextMeshProUGUI inTransitText;

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

        private readonly List<PhoneOrderRowUI> _rows     = new List<PhoneOrderRowUI>();
        private readonly List<CartRowUI>       _cartRows = new List<CartRowUI>();

        private OrderCategory _category;
        private bool          _hasCategory;

        // ── LaptopSection ────────────────────────────────────────────────────────

        protected override void OnBound()
        {
            if (supplyService == null) supplyService = FindAnyObjectByType<SupplyService>();

            if (confirmButton   != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (clearCartButton != null) clearCartButton.onClick.AddListener(HandleClearCart);

            if (tabs != null)
            {
                foreach (var tab in tabs)
                {
                    if (tab?.button == null) continue;
                    OrderCategory cat = tab.category;          // local copy for the closure
                    tab.button.onClick.AddListener(() => SelectCategory(cat));
                }
            }

            if (supplyService != null) supplyService.OnOrdersChanged += HandleOrdersChanged;
        }

        private void OnDestroy()
        {
            if (supplyService != null) supplyService.OnOrdersChanged -= HandleOrdersChanged;
        }

        protected override void OnShown()
        {
            Rebuild();

            // Keep the last tab across visits; first visit opens the first tab.
            if (!_hasCategory)
                _category = tabs != null && tabs.Length > 0 ? tabs[0].category : OrderCategory.StapleFood;
            SelectCategory(_category);
        }

        /// <summary>Adds one unit of <paramref name="target"/> to the cart (for "Reorder" links from other pages).</summary>
        public void AddToCart(IRestockable target, int amount = 1)
        {
            if (supplyService == null || target == null) return;
            supplyService.SetCartAmount(target, supplyService.GetCartAmount(target) + amount);

            _category    = target.Category;
            _hasCategory = true;
            if (IsShown) OnShown();
        }

        // ── Rows ─────────────────────────────────────────────────────────────────

        private void Rebuild()
        {
            BuildRows();
            RefreshTotalAndCart();
            RefreshInTransit();
        }

        private void BuildRows()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();

            if (supplyService == null || rowContainer == null || rowPrefab == null) return;

            foreach (IRestockable storage in supplyService.Restockables)
            {
                var go  = Instantiate(rowPrefab, rowContainer);
                var row = go.GetComponent<PhoneOrderRowUI>();
                if (row == null) { Destroy(go); continue; }

                row.Setup(storage, supplyService.GetCartAmount(storage), HandleRowChanged);
                _rows.Add(row);
            }
        }

        private void HandleRowChanged()
        {
            if (supplyService != null)
                foreach (var row in _rows)
                    supplyService.SetCartAmount(row.Target, row.Amount);

            RefreshTotalAndCart();
        }

        private void RefreshTotalAndCart()
        {
            float total = supplyService != null ? supplyService.CartTotal : 0f;
            if (totalText     != null) totalText.text = $"₺{total:F0}";
            if (confirmButton != null) confirmButton.interactable = supplyService != null && !supplyService.CartIsEmpty;

            RefreshCart();
        }

        // ── Cart ─────────────────────────────────────────────────────────────────

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

        // ── In transit ───────────────────────────────────────────────────────────

        private void HandleOrdersChanged()
        {
            if (IsShown) RefreshInTransit();
        }

        private void RefreshInTransit()
        {
            bool any = supplyService != null && supplyService.InTransit.Count > 0;
            if (inTransitRoot != null) inTransitRoot.SetActive(any);
            if (!any || inTransitText == null) return;

            // Merge all pending orders into one "Item xN" list.
            var merged = new Dictionary<IRestockable, int>();
            var order  = new List<IRestockable>();
            foreach (var o in supplyService.InTransit)
            {
                foreach (var line in o.lines)
                {
                    if (line.target == null) continue;
                    if (!merged.ContainsKey(line.target)) { merged[line.target] = 0; order.Add(line.target); }
                    merged[line.target] += line.amount;
                }
            }

            var sb = new StringBuilder("On the way - arrives tomorrow morning:\n");
            for (int i = 0; i < order.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"{order[i].DisplayName} x{merged[order[i]]}");
            }
            inTransitText.text = sb.ToString();
        }

        // ── Category tabs ────────────────────────────────────────────────────────

        /// <summary>Shows only rows in the given category and highlights the matching tab.</summary>
        private void SelectCategory(OrderCategory category)
        {
            _category    = category;
            _hasCategory = true;

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

        // ── Buttons ──────────────────────────────────────────────────────────────

        private void HandleConfirm()
        {
            if (supplyService == null || supplyService.CartIsEmpty) return;

            if (AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            float total = supplyService.CartTotal;

            if (!supplyService.CanAfford(total))
            {
                float balance = Panel != null && Panel.Money != null ? Panel.Money.Balance : 0f;
                Panel?.ShowModal("Insufficient balance",
                    $"This order costs ₺{total:F0} but you only have ₺{balance:F0}.\nRemove some items and try again.",
                    "OK", null);
                return;
            }

            Panel?.ShowModal("Confirm order",
                $"Total: ₺{total:F0}\nPaid now, delivered tomorrow morning.",
                "Place order", PlaceOrder,
                "Back", null);
        }

        private void PlaceOrder()
        {
            var result = supplyService.PlaceOrder();

            switch (result)
            {
                case SupplyService.OrderResult.Placed:
                    if (AudioManager.Instance) AudioManager.Instance.PlaySfx(Sfx.CashRegister);
                    Panel?.ShowToast("Order placed - arrives tomorrow morning.");
                    Rebuild();
                    SelectCategory(_category);
                    break;

                case SupplyService.OrderResult.InsufficientFunds:
                    Panel?.ShowModal("Insufficient balance", "You can't afford this order right now.", "OK", null);
                    break;
            }
        }

        private void HandleClearCart()
        {
            if (supplyService == null) return;

            if (AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            supplyService.ClearCart();
            Rebuild();
            SelectCategory(_category);
        }
    }
}
