using System;
using UnityEngine;

/// <summary>
/// Tracks the player's money balance.
/// Other systems call AddIncome() and DeductExpense() to change the balance.
/// Fires events so UI and game-over logic can react.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How much money the player starts with.")]
    public float startingMoney = 500f;

    [Tooltip("Expense configuration (rent, food, electricity costs).")]
    public ExpenseConfig expenseConfig;

    [Tooltip("Reference to the DayManager for daily/weekly expense deductions.")]
    public DayManager dayManager;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired whenever the balance changes. Passes (currentBalance, delta).</summary>
    public event Action<float, float> OnBalanceChanged;

    /// <summary>Fired when daily expenses are deducted. Passes (foodCost, electricityCost).</summary>
    public event Action<float, float> OnDailyExpensesDeducted;

    /// <summary>Fired when weekly rent is deducted. Passes (rentCost).</summary>
    public event Action<float> OnRentDeducted;

    // ── Runtime ──────────────────────────────────────────────────────────────

    /// <summary>Current money balance.</summary>
    public float Balance { get; private set; }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        Balance = startingMoney;
    }

    private void OnEnable()
    {
        if (dayManager != null)
            dayManager.OnDayEnded += OnDayEnded;
    }

    private void OnDisable()
    {
        if (dayManager != null)
            dayManager.OnDayEnded -= OnDayEnded;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Add income (e.g. from a completed order).
    /// </summary>
    public void AddIncome(float amount)
    {
        if (amount <= 0f) return;

        Balance += amount;
        Debug.Log($"[Money] +{amount:F2} (order income) → Balance: {Balance:F2}");
        OnBalanceChanged?.Invoke(Balance, amount);
    }

    /// <summary>
    /// Deduct an expense from the balance. Balance can go negative.
    /// </summary>
    public void DeductExpense(float amount, string reason = "")
    {
        if (amount <= 0f) return;

        Balance -= amount;
        Debug.Log($"[Money] -{amount:F2} ({reason}) → Balance: {Balance:F2}");
        OnBalanceChanged?.Invoke(Balance, -amount);
    }

    // ── Day-end expense logic ────────────────────────────────────────────────

    private void OnDayEnded(int dayNumber)
    {
        if (expenseConfig == null) return;

        DeductDailyExpenses();

        // Rent is weekly — deduct every 7 days
        if (dayNumber % 7 == 0)
            DeductRent();
    }

    private void DeductDailyExpenses()
    {
        float food = expenseConfig.dailyFoodCost;
        float electricity = expenseConfig.dailyElectricityCost;
        float total = food + electricity;

        if (total <= 0f) return;

        Balance -= total;
        Debug.Log($"[Money] Daily expenses: Food -{food:F2}, Electricity -{electricity:F2} → Balance: {Balance:F2}");
        OnBalanceChanged?.Invoke(Balance, -total);
        OnDailyExpensesDeducted?.Invoke(food, electricity);
    }

    private void DeductRent()
    {
        float rent = expenseConfig.weeklyRent;
        if (rent <= 0f) return;

        Balance -= rent;
        Debug.Log($"[Money] Weekly rent: -{rent:F2} → Balance: {Balance:F2}");
        OnBalanceChanged?.Invoke(Balance, -rent);
        OnRentDeducted?.Invoke(rent);
    }
}
