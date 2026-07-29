using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bufe.UI;

namespace Bufe
{
    public class DayEndScreen : MonoBehaviour
    {
        [Header("References")]
        public MoneyManager        moneyManager;
        public DayManager          dayManager;
        public EventScreenManager  eventScreenManager;
        public ExpenseConfig       expenseConfig;
        public FamilyManager       familyManager;
        public PlayerLook          playerLook;

        [Header("Screen Root")]
        public GameObject screenRoot;

        [Header("Header")]
        public TextMeshProUGUI dayLabel;

        [Header("Summary Rows")]
        public TextMeshProUGUI savingsText;
        public TextMeshProUGUI earningsText;

        [Header("Shop Expense Rows")]
        public Transform    expenseContainer;
        public GameObject   expenseRowPrefab;

        [Header("Family Section")]
        public Transform    familyContainer;
        public GameObject   familyRowPrefab;

        [Tooltip("Show the medicine-purchase row for sick members. OFF for the 5-day demo — " +
                 "the only sick member is healed through an event instead. Sick status still shows.")]
        public bool allowMedicinePurchase = false;

        [Header("Footer")]
        public TextMeshProUGUI totalText;
        public Button sleepButton;

        [Header("Restock")]
        public PhoneOrderScreen phoneOrderScreen;
        public Button           orderStockButton;

        [Header("Transition")]
        [Tooltip("Optional fade used to hide the world reset when starting the next day.")]
        public ScreenFader screenFader;

        [Header("Colors")]
        public Color positiveColor = new Color(0.20f, 0.85f, 0.20f, 1f);
        public Color negativeColor = new Color(0.85f, 0.20f, 0.20f, 1f);

        // ── Runtime ──────────────────────────────────────────────────────────────

        private int   _dayNumber;
        private float _savings;
        private float _income;

        private readonly List<(ExpenseEntry entry, ExpenseRowUI row)> _expenseRows =
            new List<(ExpenseEntry, ExpenseRowUI)>();

        private readonly List<(FamilyMemberState state, ExpenseRowUI medicineRow)> _medicineRows =
            new List<(FamilyMemberState, ExpenseRowUI)>();

        private float                      _restockCost;
        private readonly List<RestockLine> _pendingOrder = new List<RestockLine>();
        private ExpenseRowUI               _restockRow;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (screenRoot       != null) screenRoot.SetActive(false);
            if (sleepButton      != null) sleepButton.onClick.AddListener(HandleSleep);
            if (orderStockButton != null) orderStockButton.onClick.AddListener(OpenPhoneOrder);
        }

        private void OnEnable()
        {
            if (eventScreenManager != null) eventScreenManager.OnEventPhaseComplete += HandleEventPhaseComplete;

            if (phoneOrderScreen != null)
            {
                phoneOrderScreen.OnOrderConfirmed += HandleOrderConfirmed;
                phoneOrderScreen.OnCancelled      += HandleOrderCancelled;
            }
        }

        private void OnDisable()
        {
            if (eventScreenManager != null) eventScreenManager.OnEventPhaseComplete -= HandleEventPhaseComplete;

            if (phoneOrderScreen != null)
            {
                phoneOrderScreen.OnOrderConfirmed -= HandleOrderConfirmed;
                phoneOrderScreen.OnCancelled      -= HandleOrderCancelled;
            }
        }

        // ── Day-end entry point ───────────────────────────────────────────────────

        private void HandleEventPhaseComplete(int dayNumber)
        {
            _dayNumber = dayNumber;
            _income    = moneyManager != null ? moneyManager.DailyIncome : 0f;
            _savings   = moneyManager != null ? moneyManager.Balance - _income : 0f;

            Show();
        }

        // ── Screen population ─────────────────────────────────────────────────────

        private void Show()
        {
            if (playerLook  != null) playerLook.UIMode = true;
            if (screenRoot  != null) screenRoot.SetActive(true);

            if (dayLabel     != null) dayLabel.text     = $"END OF DAY {_dayNumber}";
            if (savingsText  != null) savingsText.text  = $"{_savings:F0}";
            if (earningsText != null) earningsText.text = $"+{_income:F0}";

            // Restock resets each day-end open; the row (if any) is cleared with the container below.
            _restockCost = 0f;
            _pendingOrder.Clear();
            _restockRow  = null;

            PopulateExpenseRows();
            PopulateFamilyRows();
            UpdateTotal();
        }

        private void PopulateExpenseRows()
        {
            ClearContainer(expenseContainer);
            _expenseRows.Clear();

            if (expenseConfig == null || expenseRowPrefab == null || expenseContainer == null) return;

            foreach (var expense in expenseConfig.GetDueExpenses(_dayNumber))
            {
                var row = SpawnExpenseRow(expenseContainer, expense.displayName, expense.amount, expense.isMandatory);
                if (row == null) continue;

                row.OnToggled += _ => UpdateTotal();
                _expenseRows.Add((expense, row));
            }
        }

        private void PopulateFamilyRows()
        {
            ClearContainer(familyContainer);
            _medicineRows.Clear();

            if (familyManager == null || familyRowPrefab == null || familyContainer == null) return;

            foreach (var state in familyManager.States)
            {
                if (state.IsGone) continue;

                var go  = Instantiate(familyRowPrefab, familyContainer);
                var row = go.GetComponent<FamilyRowUI>();
                if (row == null) continue;

                row.Setup(state);

                if (allowMedicinePurchase && state.NeedsMedicine)
                {
                    var medRow = row.SpawnMedicineRow(expenseRowPrefab, state.Data.medicineCost);
                    if (medRow != null)
                    {
                        medRow.OnToggled += _ => UpdateTotal();
                        _medicineRows.Add((state, medRow));
                    }
                }
            }
        }

        // ── Phone order (restock) ──────────────────────────────────────────────────

        private void OpenPhoneOrder()
        {
            if (phoneOrderScreen == null) return;

            if (screenRoot != null) screenRoot.SetActive(false);   // hide day-end while ordering
            
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            phoneOrderScreen.Open(_pendingOrder);
        }

        private void HandleOrderConfirmed(List<RestockLine> lines, float total)
        {
            _pendingOrder.Clear();
            _pendingOrder.AddRange(lines);
            _restockCost = total;

            RefreshRestockRow();

            if (screenRoot != null) screenRoot.SetActive(true);    // back to day-end
            UpdateTotal();
        }

        private void HandleOrderCancelled()
        {
            if (screenRoot != null) screenRoot.SetActive(true);    // back to day-end, no changes
        }

        private void RefreshRestockRow()
        {
            if (_restockCost > 0f)
            {
                if (_restockRow == null)
                    _restockRow = SpawnExpenseRow(expenseContainer, "RESTOCKING", _restockCost, isMandatory: true);
                else
                    _restockRow.SetAmount(_restockCost);
            }
            else if (_restockRow != null)
            {
                Destroy(_restockRow.gameObject);
                _restockRow = null;
            }
        }

        // ── Footer update ─────────────────────────────────────────────────────────

        private void UpdateTotal()
        {
            float deductions = 0f;

            foreach (var (entry, row) in _expenseRows)
                if (row.IsEnabled) deductions += entry.amount;

            foreach (var (state, row) in _medicineRows)
                if (row.IsEnabled) deductions += state.Data.medicineCost;

            deductions += _restockCost;

            float newBalance = _savings + _income - deductions;

            if (totalText != null)
            {
                totalText.text  = $"{newBalance:F0}";
                totalText.color = newBalance >= 0f ? positiveColor : negativeColor;
            }
        }

        // ── Sleep button ──────────────────────────────────────────────────────────

        public void HandleSleep()
        {
            bool foodPaid = false;
            bool heatPaid = false;

            if (moneyManager != null)
            {
                // Pay shop expenses and detect food/heat state
                foreach (var (entry, row) in _expenseRows)
                {
                    if (!row.IsEnabled) continue;
                    moneyManager.DeductExpense(entry.amount, entry.displayName);

                    if (entry.needType == ExpenseNeedType.Food) foodPaid = true;
                    if (entry.needType == ExpenseNeedType.Heat) heatPaid = true;
                }

                // Pay medicine per member
                foreach (var (state, row) in _medicineRows)
                {
                    if (row.IsEnabled)
                        moneyManager.DeductExpense(state.Data.medicineCost, "medicine");
                }

                moneyManager.ResetDailyIncome();
            }

            // Apply the restock order: add stock now and pay for it.
            if (_restockCost > 0f)
            {
                foreach (var line in _pendingOrder)
                    line.target?.AddStock(line.amount);

                moneyManager?.DeductExpense(_restockCost, "Restocking");
            }

            // Build the set of members who received medicine
            var medicated = new HashSet<FamilyMemberState>();
            foreach (var (state, row) in _medicineRows)
                if (row.IsEnabled) medicated.Add(state);

            if (familyManager != null)
                familyManager.ProcessDayEnd(foodPaid, heatPaid, medicated);
            
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            StartCoroutine(FinishSleep());
        }

        /// <summary>
        /// Fades to black, starts the next day (which teleports the player and
        /// clears leftover items under the cover of the fade), closes the screen,
        /// then fades back in on the fresh morning.
        /// </summary>
        private IEnumerator FinishSleep()
        {
            if (screenFader != null) yield return screenFader.FadeOut();

            // If anything in the new-day reset chain throws, we still MUST close the
            // screen and release the player — otherwise UIMode stays true and the
            // player is stuck unable to move with a free cursor.
            try
            {
                if (dayManager != null)
                    dayManager.BeginNextDay();      // resets the world while black
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DayEndScreen] Exception while starting the next day: {e}");
            }

            if (screenRoot  != null) screenRoot.SetActive(false);
            if (playerLook  != null) playerLook.UIMode = false;

            if (screenFader != null) yield return screenFader.FadeIn();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private ExpenseRowUI SpawnExpenseRow(Transform container, string label, float amount, bool isMandatory)
        {
            if (expenseRowPrefab == null || container == null) return null;
            var go  = Instantiate(expenseRowPrefab, container);
            var row = go.GetComponent<ExpenseRowUI>();
            row?.Setup(label, amount, isMandatory);
            return row;
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;
            foreach (Transform child in container)
                Destroy(child.gameObject);
        }

    }
}
