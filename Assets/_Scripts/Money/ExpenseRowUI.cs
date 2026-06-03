using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single row in the day-end screen expense list.
/// Mandatory rows show the amount in red and have no toggle.
/// Preference rows have a toggle button; toggling off grays the row and skips the deduction.
///
/// Prefab structure:
///   ExpenseRow (this component)
///     ├─ LabelText      (TextMeshProUGUI)
///     ├─ AmountText     (TextMeshProUGUI)
///     └─ ToggleRoot     (GameObject — only visible for preference expenses)
///            └─ Button  (Button component — calls Toggle())
/// </summary>
public class ExpenseRowUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI amountText;

    [Tooltip("Parent object for the toggle button. Hidden for mandatory expenses.")]
    public GameObject toggleRoot;

    [Header("Colors")]
    public Color activeColor   = new Color(0.85f, 0.15f, 0.15f, 1f);
    public Color inactiveColor = new Color(0.40f, 0.40f, 0.40f, 1f);

    /// <summary>Whether this expense is currently set to be deducted.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Fired when the player toggles this row. Passes the new enabled state.</summary>
    public event Action<bool> OnToggled;

    private float _amount;

    // ── Setup ─────────────────────────────────────────────────────────────────

    /// <summary>Initialize the row. Call once after Instantiate.</summary>
    public void Setup(string label, float amount, bool isMandatory)
    {
        _amount   = amount;
        IsEnabled = true;

        if (labelText  != null) labelText.text = label;

        if (toggleRoot != null)
        {
            toggleRoot.SetActive(!isMandatory);

            var btn = toggleRoot.GetComponentInChildren<Button>();
            if (btn != null) btn.onClick.AddListener(Toggle);
        }

        Refresh();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Toggle enabled state. Wired to the row's Button via Setup().</summary>
    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        Refresh();
        OnToggled?.Invoke(IsEnabled);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void Refresh()
    {
        if (amountText == null) return;

        Color c = IsEnabled ? activeColor : inactiveColor;

        amountText.text  = IsEnabled ? $"-{_amount:F0}" : $"({_amount:F0})";
        amountText.color = c;

        if (labelText != null)
            labelText.color = c;
    }
}
