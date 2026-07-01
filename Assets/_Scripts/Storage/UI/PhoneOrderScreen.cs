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

        /// <summary>Player confirmed an order. Passes the pending lines and total cost.</summary>
        public event Action<List<RestockLine>, float> OnOrderConfirmed;

        /// <summary>Player cancelled — no changes.</summary>
        public event Action OnCancelled;

        private readonly List<PhoneOrderRowUI> _rows = new List<PhoneOrderRowUI>();

        private void Start()
        {
            if (screenRoot    != null) screenRoot.SetActive(false);
            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (cancelButton  != null) cancelButton.onClick.AddListener(HandleCancel);
        }

        /// <summary>Opens the screen, pre-filling order amounts from the current pending order.</summary>
        public void Open(IReadOnlyList<RestockLine> pending)
        {
            BuildRows(pending);
            UpdateTotal();
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

                row.Setup(storage, LookupPending(pending, storage), UpdateTotal);
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

        private void UpdateTotal()
        {
            float total = 0f;
            foreach (var row in _rows) total += row.Subtotal;
            if (totalText != null) totalText.text = $"{total:F0}$";
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

            Close();
            OnOrderConfirmed?.Invoke(lines, total);
        }

        private void HandleCancel()
        {
            Close();
            OnCancelled?.Invoke();
        }
    }
}
