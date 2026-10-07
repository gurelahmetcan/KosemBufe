using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public enum ExpenseFrequency { Daily, Weekly }

    /// <summary>
    /// Tags a shop expense so DayEndScreen can tell FamilyManager
    /// whether food or heat was paid this day.
    /// </summary>
    public enum ExpenseNeedType { None, Food, Heat }

    [Serializable]
    public class ExpenseEntry
    {
        [Tooltip("Day-end label.")]
        public string displayName = "Expense";

        [Min(0f)]
        public float amount;

        [Tooltip("Daily or every 7 days.")]
        public ExpenseFrequency frequency = ExpenseFrequency.Daily;

        [Tooltip("Always paid, can't be toggled.")]
        public bool isMandatory = true;

        [Tooltip("Family need this expense covers.")]
        public ExpenseNeedType needType = ExpenseNeedType.None;
    }

    /// <summary>
    /// Editable list of recurring shop expenses shown on the day-end screen.
    /// Create via: Assets → Create → Kosem Bufe → Expense Config
    /// </summary>
    [CreateAssetMenu(fileName = "ExpenseConfig", menuName = "Kosem Bufe/Expense Config")]
    public class ExpenseConfig : ScriptableObject
    {
        public List<ExpenseEntry> expenses = new List<ExpenseEntry>();

        private void Reset()
        {
            expenses = new List<ExpenseEntry>
            {
                new ExpenseEntry { displayName = "RENT",          amount = 200f, frequency = ExpenseFrequency.Weekly, isMandatory = true,  needType = ExpenseNeedType.None },
                new ExpenseEntry { displayName = "BÜFE EXPENSES", amount = 50f,  frequency = ExpenseFrequency.Daily,  isMandatory = true,  needType = ExpenseNeedType.None },
                new ExpenseEntry { displayName = "FOOD",          amount = 30f,  frequency = ExpenseFrequency.Daily,  isMandatory = false, needType = ExpenseNeedType.Food },
                new ExpenseEntry { displayName = "HEAT",          amount = 20f,  frequency = ExpenseFrequency.Daily,  isMandatory = false, needType = ExpenseNeedType.Heat },
            };
        }

        /// <summary>Returns all expenses due at the end of the given day number.</summary>
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
}
