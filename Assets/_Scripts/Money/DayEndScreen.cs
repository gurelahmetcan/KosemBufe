using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Papers-Please-style day-end summary screen.
///
/// Shows: savings (carry-over balance), today's earnings, each due expense,
/// a running total, and a Sleep button that commits deductions and starts the next day.
///
/// Mandatory expenses are always deducted.
/// Preference expenses have a toggle button — the player can opt out.
///
/// Scene / Canvas setup:
///   Assign ScreenRoot to the panel GameObject that should be hidden/shown.
///   Assign ExpenseRowPrefab — a prefab with an ExpenseRowUI component (see that class for prefab structure).
///   Wire the Sleep button's OnClick to HandleSleep().
///
/// Required references: MoneyManager, DayManager, ExpenseConfig.
/// </summary>
public class DayEndScreen : MonoBehaviour
{
    [Header("References")]
    public MoneyManager moneyManager;
    public DayManager   dayManager;
    public ExpenseConfig expenseConfig;

    [Header("Screen Root")]
    [Tooltip("The panel GameObject to show/hide. Can be this GameObject or a child.")]
    public GameObject screenRoot;

    [Header("Header")]
    public TextMeshProUGUI dayLabel;

    [Header("Summary Rows")]
    [Tooltip("Displays the carry-over balance from before today's earnings.")]
    public TextMeshProUGUI savingsText;

    [Tooltip("Displays today's total order earnings.")]
    public TextMeshProUGUI earningsText;

    [Header("Expense Rows")]
    [Tooltip("Parent transform (e.g. VerticalLayoutGroup) where expense rows are spawned.")]
    public Transform expenseContainer;

    [Tooltip("Prefab with an ExpenseRowUI component.")]
    public GameObject expenseRowPrefab;

    [Header("Footer")]
    public TextMeshProUGUI totalText;
    public Button sleepButton;

    [Header("Colors")]
    public Color positiveColor = new Color(0.20f, 0.85f, 0.20f, 1f);
    public Color negativeColor = new Color(0.85f, 0.20f, 0.20f, 1f);

    // ── Runtime ──────────────────────────────────────────────────────────────

    private int   _dayNumber;
    private float _savings;
    private float _income;

    private readonly List<(ExpenseEntry entry, ExpenseRowUI row)> _expenseRows =
        new List<(ExpenseEntry, ExpenseRowUI)>();

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        if (screenRoot != null) screenRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (dayManager != null)
            dayManager.OnDayEnded += HandleDayEnded;
    }

    private void OnDisable()
    {
        if (dayManager != null)
            dayManager.OnDayEnded -= HandleDayEnded;
    }

    // ── Day-end entry point ───────────────────────────────────────────────────

    private void HandleDayEnded(int dayNumber)
    {
        _dayNumber = dayNumber;
        _income    = moneyManager != null ? moneyManager.DailyIncome : 0f;
        _savings   = moneyManager != null ? moneyManager.Balance - _income : 0f;

        Show();
    }

    // ── Screen population ─────────────────────────────────────────────────────

    private void Show()
    {
        if (screenRoot != null) screenRoot.SetActive(true);

        if (dayLabel    != null) dayLabel.text    = $"END OF DAY {_dayNumber}";
        if (savingsText != null) savingsText.text  = $"{_savings:F0}";
        if (earningsText != null) earningsText.text = $"+{_income:F0}";

        PopulateExpenseRows();
        UpdateTotal();
    }

    private void PopulateExpenseRows()
    {
        foreach (Transform child in expenseContainer)
            Destroy(child.gameObject);

        _expenseRows.Clear();

        if (expenseConfig == null || expenseRowPrefab == null || expenseContainer == null) return;

        foreach (var expense in expenseConfig.GetDueExpenses(_dayNumber))
        {
            var go  = Instantiate(expenseRowPrefab, expenseContainer);
            var row = go.GetComponent<ExpenseRowUI>();
            if (row == null) continue;

            row.Setup(expense.displayName, expense.amount, expense.isMandatory);
            row.OnToggled += _ => UpdateTotal();

            _expenseRows.Add((expense, row));
        }
    }

    private void UpdateTotal()
    {
        float totalDeductions = 0f;
        foreach (var (entry, row) in _expenseRows)
        {
            if (row.IsEnabled) totalDeductions += entry.amount;
        }

        float newBalance = _savings + _income - totalDeductions;

        if (totalText != null)
        {
            totalText.text  = $"${newBalance:F0}";
            totalText.color = newBalance >= 0f ? positiveColor : negativeColor;
        }
    }

    // ── Sleep button ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the Sleep button's OnClick.
    /// Deducts all active expenses, resets daily income, and starts the next day.
    /// </summary>
    public void HandleSleep()
    {
        if (moneyManager != null)
        {
            foreach (var (entry, row) in _expenseRows)
            {
                if (row.IsEnabled)
                    moneyManager.DeductExpense(entry.amount, entry.displayName);
            }

            moneyManager.ResetDailyIncome();
        }

        if (dayManager != null)
            dayManager.BeginNextDay();

        if (screenRoot != null) screenRoot.SetActive(false);
    }
}
