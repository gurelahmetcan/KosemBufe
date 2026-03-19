using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays customer dialogue in a world-space or screen-space text bubble.
///
/// Normal customer flow:
///   1. ShowOpeningLine()     → customer spawns, says hello
///   2. PlayOrderSequence()   → player clicks, customer states order (+ modifier)
///   3. ShowExitLine()        → after delivery or timeout, customer reacts and leaves
///
/// Scene Setup:
///   1. Attach to a UI GameObject (e.g. a panel with a TextMeshProUGUI child).
///   2. Assign the dialogueText reference.
///   3. The panel starts hidden and is shown/hidden as dialogue plays.
/// </summary>
public class CustomerDialogueUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The panel/container that holds the dialogue bubble.")]
    public GameObject panel;

    [Tooltip("Text element that displays the dialogue line.")]
    public TextMeshProUGUI dialogueText;

    [Header("Timing")]
    [Tooltip("Seconds each dialogue line stays visible before advancing.")]
    public float lineDisplayDuration = 2f;

    [Tooltip("Short pause between consecutive lines.")]
    public float linePauseDuration = 0.3f;

    /// <summary>True while dialogue is being displayed.</summary>
    public bool IsPlaying { get; private set; }

    private Coroutine _activeSequence;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the opening line when the customer spawns.
    /// Stays visible until hidden or replaced by another call.
    /// </summary>
    public void ShowOpeningLine(string line)
    {
        StopActive();
        _activeSequence = StartCoroutine(ShowLineRoutine(line, lineDisplayDuration));
    }

    /// <summary>
    /// Plays the order sequence: order line → optional modifier line.
    /// Called when the player clicks the customer to take the order.
    /// </summary>
    public void PlayOrderSequence(DialogueBuilder.OrderDialogue dialogue)
    {
        StopActive();
        _activeSequence = StartCoroutine(OrderSequenceRoutine(dialogue));
    }

    /// <summary>
    /// Shows the exit line (positive, negative, or timeout).
    /// Called after delivery result or when the customer leaves.
    /// </summary>
    public void ShowExitLine(string line)
    {
        StopActive();
        _activeSequence = StartCoroutine(ShowLineRoutine(line, lineDisplayDuration));
    }

    /// <summary>
    /// Shows a single line of dialogue for the configured duration.
    /// </summary>
    public void ShowLine(string line, float duration = -1f)
    {
        StopActive();
        _activeSequence = StartCoroutine(ShowLineRoutine(line, duration < 0 ? lineDisplayDuration : duration));
    }

    /// <summary>
    /// Immediately hides dialogue and stops any active sequence.
    /// </summary>
    public void Hide()
    {
        StopActive();
        IsPlaying = false;
        HidePanel();
    }

    // ── Coroutines ────────────────────────────────────────────────────────

    private IEnumerator ShowLineRoutine(string line, float duration)
    {
        IsPlaying = true;
        SetText(line);
        ShowPanel();

        yield return new WaitForSeconds(duration);

        HidePanel();
        IsPlaying = false;
        _activeSequence = null;
    }

    private IEnumerator OrderSequenceRoutine(DialogueBuilder.OrderDialogue dialogue)
    {
        IsPlaying = true;

        // Order line
        SetText(dialogue.order);
        ShowPanel();
        yield return new WaitForSeconds(lineDisplayDuration);

        // Modifier (if any)
        if (!string.IsNullOrEmpty(dialogue.modifier))
        {
            yield return new WaitForSeconds(linePauseDuration);
            SetText(dialogue.modifier);
            yield return new WaitForSeconds(lineDisplayDuration);
        }

        HidePanel();
        IsPlaying = false;
        _activeSequence = null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void StopActive()
    {
        if (_activeSequence != null)
        {
            StopCoroutine(_activeSequence);
            _activeSequence = null;
        }
    }

    private void SetText(string text)
    {
        if (dialogueText != null)
            dialogueText.text = text;
    }

    private void ShowPanel()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    private void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
