using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays customer dialogue in a world-space or screen-space text bubble.
/// Handles the sequential flow: opening → order (+ modifier) → reaction → exit.
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

    /// <summary>
    /// Shows a single line of dialogue for the configured duration.
    /// </summary>
    public void ShowLine(string line, float duration = -1f)
    {
        if (_activeSequence != null)
            StopCoroutine(_activeSequence);

        _activeSequence = StartCoroutine(ShowLineRoutine(line, duration < 0 ? lineDisplayDuration : duration));
    }

    /// <summary>
    /// Plays the normal customer opening sequence: opening line → order (+ modifier).
    /// Called when the customer first appears.
    /// </summary>
    public void PlayOpeningSequence(DialogueBuilder.NormalDialogue dialogue)
    {
        if (_activeSequence != null)
            StopCoroutine(_activeSequence);

        _activeSequence = StartCoroutine(OpeningSequenceRoutine(dialogue));
    }

    /// <summary>
    /// Shows a reaction line (after order delivery) then the exit line.
    /// </summary>
    public void PlayReactionSequence(string reactionLine, string exitLine)
    {
        if (_activeSequence != null)
            StopCoroutine(_activeSequence);

        _activeSequence = StartCoroutine(ReactionSequenceRoutine(reactionLine, exitLine));
    }

    /// <summary>
    /// Immediately hides dialogue and stops any active sequence.
    /// </summary>
    public void Hide()
    {
        if (_activeSequence != null)
        {
            StopCoroutine(_activeSequence);
            _activeSequence = null;
        }

        IsPlaying = false;

        if (panel != null)
            panel.SetActive(false);
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

    private IEnumerator OpeningSequenceRoutine(DialogueBuilder.NormalDialogue dialogue)
    {
        IsPlaying = true;

        // Opening line
        SetText(dialogue.opening);
        ShowPanel();
        yield return new WaitForSeconds(lineDisplayDuration);

        // Order line
        yield return new WaitForSeconds(linePauseDuration);
        SetText(dialogue.order);
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

    private IEnumerator ReactionSequenceRoutine(string reactionLine, string exitLine)
    {
        IsPlaying = true;

        // Reaction
        SetText(reactionLine);
        ShowPanel();
        yield return new WaitForSeconds(lineDisplayDuration);

        // Exit line
        yield return new WaitForSeconds(linePauseDuration);
        SetText(exitLine);
        yield return new WaitForSeconds(lineDisplayDuration);

        HidePanel();
        IsPlaying = false;
        _activeSequence = null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

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
