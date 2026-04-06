using UnityEngine;

/// <summary>
/// Editable configuration for recurring expenses.
/// Create via: Assets → Create → Kosem Bufe → Expense Config
/// </summary>
[CreateAssetMenu(fileName = "ExpenseConfig", menuName = "Kosem Bufe/Expense Config")]
public class ExpenseConfig : ScriptableObject
{
    [Header("Weekly Expenses")]
    [Tooltip("Rent cost deducted every 7 days.")]
    [Min(0f)]
    public float weeklyRent = 200f;

    [Header("Daily Expenses")]
    [Tooltip("Food supply cost deducted at the end of every day.")]
    [Min(0f)]
    public float dailyFoodCost = 30f;

    [Tooltip("Electricity cost deducted at the end of every day.")]
    [Min(0f)]
    public float dailyElectricityCost = 20f;

    /// <summary>Total daily expense (food + electricity).</summary>
    public float TotalDailyCost => dailyFoodCost + dailyElectricityCost;
}
