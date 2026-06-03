using System;
using UnityEngine;

/// <summary>
/// Tracks the player's money balance and today's earnings.
/// Expense deduction is driven by DayEndScreen — this class only provides AddIncome / DeductExpense primitives.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Fallback starting money if GameManager/GameConfig is not present.")]
    public float startingMoney = 500f;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired whenever the balance changes. Passes (currentBalance, delta).</summary>
    public event Action<float, float> OnBalanceChanged;

    /// <summary>Fired once when the balance reaches 0 or below.</summary>
    public event Action OnBankrupt;

    // ── Runtime ──────────────────────────────────────────────────────────────

    /// <summary>Current money balance.</summary>
    public float Balance { get; private set; }

    /// <summary>Total income earned from orders since the last ResetDailyIncome call.</summary>
    public float DailyIncome { get; private set; }

    /// <summary>True after the player has gone bankrupt.</summary>
    public bool IsBankrupt { get; private set; }

    // ── Debug ────────────────────────────────────────────────────────────────

    [Header("Debug")]
    public float debugAmount = 50f;

#if UNITY_EDITOR
    [ContextMenu("Debug: Add Money")]
    private void DebugAddMoney() => AddIncome(debugAmount);

    [ContextMenu("Debug: Subtract Money")]
    private void DebugSubtractMoney() => DeductExpense(debugAmount, "debug");
#endif

    // ── Init (called by GameManager) ─────────────────────────────────────────

    private bool _initialized;

    /// <summary>Called by GameManager.ApplyConfig() to set the starting balance.</summary>
    public void Init(float starting)
    {
        Balance = starting;
        _initialized = true;
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (!_initialized)
            Balance = startingMoney;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Add income (e.g. from a completed order). Also accumulates DailyIncome.</summary>
    public void AddIncome(float amount)
    {
        if (amount <= 0f) return;

        Balance += amount;
        DailyIncome += amount;
        Debug.Log($"[Money] +{amount:F2} (order income) → Balance: {Balance:F2}");
        OnBalanceChanged?.Invoke(Balance, amount);
    }

    /// <summary>Deduct an expense from the balance. Triggers bankruptcy if balance hits 0.</summary>
    public void DeductExpense(float amount, string reason = "")
    {
        if (amount <= 0f) return;

        Balance -= amount;
        Debug.Log($"[Money] -{amount:F2} ({reason}) → Balance: {Balance:F2}");
        OnBalanceChanged?.Invoke(Balance, -amount);
        CheckBankrupt();
    }

    /// <summary>Resets the daily income counter. Called by DayEndScreen after the player sleeps.</summary>
    public void ResetDailyIncome()
    {
        DailyIncome = 0f;
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void CheckBankrupt()
    {
        if (IsBankrupt || Balance > 0f) return;

        IsBankrupt = true;
        Debug.Log("[Money] BANKRUPT — balance reached 0!");
        OnBankrupt?.Invoke();
    }
}
