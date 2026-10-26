using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Owns restocking: the list of orderable storages, the player's cart, orders in transit
    /// and delivery. Orders are paid immediately from the balance and delivered the next
    /// morning (on DayManager.OnNewDayReset). UI-agnostic - the laptop's restock section
    /// reads and writes through this.
    /// </summary>
    public class SupplyService : MonoBehaviour
    {
        public enum OrderResult { Placed, EmptyCart, InsufficientFunds }

        /// <summary>A placed order waiting to be delivered.</summary>
        public class SupplyOrder
        {
            public readonly List<RestockLine> lines;
            public readonly float total;
            public readonly int   deliverDay;

            public SupplyOrder(List<RestockLine> lines, float total, int deliverDay)
            {
                this.lines      = lines;
                this.total      = total;
                this.deliverDay = deliverDay;
            }
        }

        [Header("References")]
        public DayManager   dayManager;
        public MoneyManager moneyManager;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when an order is placed or delivered.</summary>
        public event Action OnOrdersChanged;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private readonly List<IRestockable>            _restockables = new List<IRestockable>();
        private readonly Dictionary<IRestockable, int> _cart         = new Dictionary<IRestockable, int>();
        private readonly List<SupplyOrder>             _inTransit    = new List<SupplyOrder>();

        /// <summary>Every finite (orderable) storage in the scene, sorted by category then name.</summary>
        public IReadOnlyList<IRestockable> Restockables => _restockables;

        /// <summary>Orders placed but not yet delivered.</summary>
        public IReadOnlyList<SupplyOrder> InTransit => _inTransit;

        /// <summary>Money spent on orders since the current day started. Shown on the day-end screen.</summary>
        public float SpentToday { get; private set; }

        public bool CartIsEmpty
        {
            get
            {
                foreach (var amount in _cart.Values)
                    if (amount > 0) return false;
                return true;
            }
        }

        public float CartTotal
        {
            get
            {
                float total = 0f;
                foreach (var kv in _cart)
                    total += kv.Key.UnitCost * kv.Value;
                return total;
            }
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (dayManager   == null) dayManager   = FindAnyObjectByType<DayManager>();
            if (moneyManager == null) moneyManager = FindAnyObjectByType<MoneyManager>();

            DiscoverRestockables();
        }

        private void OnEnable()
        {
            if (dayManager != null) dayManager.OnNewDayReset += HandleNewDay;
        }

        private void OnDisable()
        {
            if (dayManager != null) dayManager.OnNewDayReset -= HandleNewDay;
        }

        // ── Cart ─────────────────────────────────────────────────────────────────

        public int GetCartAmount(IRestockable target)
        {
            return target != null && _cart.TryGetValue(target, out int amount) ? amount : 0;
        }

        public void SetCartAmount(IRestockable target, int amount)
        {
            if (target == null) return;
            if (amount <= 0) _cart.Remove(target);
            else             _cart[target] = amount;
        }

        public void ClearCart() => _cart.Clear();

        /// <summary>True if paying <paramref name="total"/> leaves a positive balance (0 = bankrupt).</summary>
        public bool CanAfford(float total)
        {
            return moneyManager == null || moneyManager.Balance - total > 0f;
        }

        // ── Ordering ─────────────────────────────────────────────────────────────

        /// <summary>Pays for the cart and queues it for delivery tomorrow morning.</summary>
        public OrderResult PlaceOrder()
        {
            if (CartIsEmpty) return OrderResult.EmptyCart;

            float total = CartTotal;
            if (!CanAfford(total)) return OrderResult.InsufficientFunds;

            var lines = new List<RestockLine>();
            foreach (var kv in _cart)
                if (kv.Value > 0) lines.Add(new RestockLine(kv.Key, kv.Value));

            int today = dayManager != null ? dayManager.CurrentDay : 0;
            _inTransit.Add(new SupplyOrder(lines, total, today + 1));

            moneyManager?.DeductExpense(total, "Restocking");
            SpentToday += total;
            _cart.Clear();

            Log.Info($"[Supply] Order placed: {lines.Count} lines, ₺{total:F0}, arrives day {today + 1}.");
            OnOrdersChanged?.Invoke();
            return OrderResult.Placed;
        }

        // ── Delivery ─────────────────────────────────────────────────────────────

        /// <summary>New morning: deliver due orders and start a fresh spending tally.</summary>
        private void HandleNewDay()
        {
            SpentToday = 0f;
            _cart.Clear();

            int today = dayManager != null ? dayManager.CurrentDay : int.MaxValue;
            bool delivered = false;

            for (int i = _inTransit.Count - 1; i >= 0; i--)
            {
                var order = _inTransit[i];
                if (order.deliverDay > today) continue;

                foreach (var line in order.lines)
                    line.target?.AddStock(line.amount);

                _inTransit.RemoveAt(i);
                delivered = true;
            }

            if (!delivered) return;

            NotificationService.Instance?.Add("Your supply order has been delivered.", NotificationKind.Success);
            OnOrdersChanged?.Invoke();
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void DiscoverRestockables()
        {
            _restockables.Clear();

            foreach (var s in FindObjectsByType<IngredientStorage>(FindObjectsSortMode.None))
                if (!s.IsInfinite) _restockables.Add(s);

            foreach (var s in FindObjectsByType<PremadeStorage>(FindObjectsSortMode.None))
                if (!s.IsInfinite) _restockables.Add(s);

            _restockables.Sort((a, b) =>
            {
                int byCategory = a.Category.CompareTo(b.Category);
                return byCategory != 0 ? byCategory : string.CompareOrdinal(a.DisplayName, b.DisplayName);
            });
        }
    }
}
