using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD bar that displays the current reputation.
/// Assign the ReputationManager reference and wire up the fill Image + text.
///
/// Scene Setup:
///   Canvas (Screen Space – Overlay)
///     └─ ReputationPanel
///         └─ Background Image
///             └─ Fill Image (type: Filled, method: Horizontal)
///         └─ ReputationText (TextMeshProUGUI, e.g. "50 / 100")
/// </summary>
public class ReputationUI : MonoBehaviour
{
    [Header("References")]
    public ReputationManager reputationManager;

    [Tooltip("Filled Image representing the reputation bar.")]
    public Image fillBar;

    [Tooltip("Optional text showing numeric value.")]
    public TextMeshProUGUI valueText;

    [Header("Colors")]
    public Color highColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color midColor  = new Color(0.9f, 0.8f, 0.2f, 1f);
    public Color lowColor  = new Color(0.9f, 0.2f, 0.2f, 1f);

    private void OnEnable()
    {
        if (reputationManager != null)
            reputationManager.OnReputationChanged += OnReputationChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (reputationManager != null)
            reputationManager.OnReputationChanged -= OnReputationChanged;
    }

    private void OnReputationChanged(int current, int delta)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (reputationManager == null) return;

        float ratio = reputationManager.Reputation / (float)ReputationManager.MaxReputation;

        if (fillBar != null)
        {
            fillBar.fillAmount = ratio;
            fillBar.color = ratio > 0.5f ? highColor : ratio > 0.25f ? midColor : lowColor;
        }

        if (valueText != null)
            valueText.text = $"{reputationManager.Reputation} / {ReputationManager.MaxReputation}";
    }
}
