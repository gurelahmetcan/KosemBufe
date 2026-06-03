using System;
using System.Collections.Generic;
using UnityEngine;

public enum ExpenseFrequency { Daily, Weekly }

[Serializable]
public class ExpenseEntry
{
    [Tooltip("Label shown in the day-end screen.")]
    public string displayName = "Expense";

    [Min(0f)]
    public float amount;

    [Tooltip("Daily = deducted every night. Weekly = deducted every 7 days.")]
    public ExpenseFrequency frequency = ExpenseFrequency.Daily;

    [Tooltip("Mandatory expenses are always deducted. Preference expenses can be toggled off by the player.")]
    public bool isMandatory = true;
}

/// <summary>
/// Editable list of recurring expenses shown on the day-end screen.
/// Create via: Assets → Create → Kosem Bufe → Expense Config
/// </summary>
[CreateAssetMenu(fileName = "ExpenseConfig", menuName = "Kosem Bufe/Expense Config")]
public class ExpenseConfig : ScriptableObject
{
    [Tooltip("All recurring expenses. Configured per-entry as mandatory or preference.")]
    public List<ExpenseEntry> expenses = new List<ExpenseEntry>();

    // Populates sensible defaults when the asset is first created.
    private void Reset()
    {
        expenses = new List<ExpenseEntry>
        {
            new ExpenseEntry { displayName = "RENT",           amount = 200f, frequency = ExpenseFrequency.Weekly, isMandatory = true  },
            new ExpenseEntry { displayName = "BÜFE EXPENSES",  amount = 50f,  frequency = ExpenseFrequency.Daily,  isMandatory = true  },
            new ExpenseEntry { displayName = "FOOD",           amount = 30f,  frequency = ExpenseFrequency.Daily,  isMandatory = false },
            new ExpenseEntry { displayName = "HEAT",           amount = 20f,  frequency = ExpenseFrequency.Daily,  isMandatory = false },
        };
    }

    /// <summary>Returns all expenses that are due at the end of the given day.</summary>
    public List<ExpenseEntry> GetDueExpenses(int dayNumber)
    {
        var due = new List<ExpenseEntry>();
        foreach (var e in expenses)
        {
            bool isDue = e.frequency == ExpenseFrequency.Daily ||
                        (e.frequency == ExpenseFrequency.Weekly && dayNumber % 7 == 0);
            if (isDue) due.Add(e);
        }
        return due;
    }
}
