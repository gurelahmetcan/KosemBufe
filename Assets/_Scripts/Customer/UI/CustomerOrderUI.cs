using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Unified UI that handles both customer dialogue bubbles and the order
/// accept/reject/close prompt — on a single panel.
///
/// Dialogue-only moments (opening line, exit line) show only the text.
/// The order prompt shows text + action buttons so the player can decide.
///
/// Scene Setup:
///   1. Create a Canvas with a panel child.
///   2. Inside the panel add:
///        - A TextMeshProUGUI for dialogue / recipe text.
///        - Three Buttons: Accept, Reject, Close.
///   3. Drag references into this component's fields.
///   The panel starts hidden.
/// </summary>
public class CustomerOrderUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The panel that contains all UI elements.")]
    public GameObject panel;

    [Tooltip("Text element for dialogue lines and recipe names.")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("Button to accept the order.")]
    public Button acceptButton;

    [Tooltip("Button to reject the order (customer leaves).")]
    public Button rejectButton;

    [Tooltip("Button to close the prompt without deciding.")]
    public Button closeButton;

    [Header("Timing")]
    [Tooltip("Seconds each dialogue line stays visible before auto-hiding.")]
    public float lineDisplayDuration = 2f;

    [Tooltip("Short pause between consecutive lines.")]
    public float linePauseDuration = 0.3f;

    /// <summary>Fired when the player clicks Accept.</summary>
    public event Action OnAccept;

    /// <summary>Fired when the player clicks Reject.</summary>
    public event Action OnReject;

    /// <summary>Fired when the player clicks Close.</summary>
    public event Action OnClose;

    /// <summary>True while the prompt buttons are visible.</summary>
    public bool IsPromptVisible => panel != null && panel.activeSelf && _buttonsVisible;

    /// <summary>True while dialogue text is being displayed (timed).</summary>
    public bool IsPlaying { get; private set; }

    private Coroutine _activeSequence;
    private bool _buttonsVisible;

    private void Awake()
    {
        if (acceptButton != null) acceptButton.onClick.AddListener(HandleAccept);
        if (rejectButton != null) rejectButton.onClick.AddListener(HandleReject);
        if (closeButton != null)  closeButton.onClick.AddListener(HandleClose);

        Hide();
    }

    // ── Dialogue API (text only, auto-hides after duration) ──────────────

    /// <summary>
    /// Shows the opening line when the customer spawns.
    /// </summary>
    public void ShowOpeningLine(string line)
    {
        StopActive();
        ShowPanel();
        _activeSequence = StartCoroutine(ShowLineRoutine(line, lineDisplayDuration));
    }

    /// <summary>
    /// Plays the order sequence: order line → optional modifier line.
    /// Buttons are NOT shown during this — they appear via ShowPrompt().
    /// </summary>
    public void PlayOrderSequence(DialogueBuilder.OrderDialogue dialogue)
    {
        StopActive();
        ShowPanel();
        _activeSequence = StartCoroutine(OrderSequenceRoutine(dialogue));
    }

    /// <summary>
    /// Shows the exit line (positive, negative, or timeout).
    /// </summary>
    public void ShowExitLine(string line)
    {
        StopActive();
        ShowPanel();
        _activeSequence = StartCoroutine(ShowLineRoutine(line, lineDisplayDuration));
    }

    /// <summary>
    /// Shows a single dialogue line for the given duration.
    /// </summary>
    public void ShowLine(string line, float duration = -1f)
    {
        StopActive();
        ShowPanel();
        _activeSequence = StartCoroutine(ShowLineRoutine(line, duration < 0 ? lineDisplayDuration : duration));
    }

    // ── Prompt API (text + buttons, stays until player clicks) ──────────

    /// <summary>
    /// Shows the order prompt with recipe text and Accept/Reject/Close buttons.
    /// Stays visible until the player clicks a button.
    /// </summary>
    public void ShowPrompt(string recipe)
    {
        StopActive();
        SetText(recipe);
        SetButtonsVisible(true);
        ShowPanel();
    }

    /// <summary>
    /// Hides the prompt buttons (panel stays if dialogue is still playing).
    /// </summary>
    public void HidePrompt()
    {
        SetButtonsVisible(false);
        HidePanel();
    }

    // ── Shared ───────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately hides everything and stops any active sequence.
    /// </summary>
    public void Hide()
    {
        StopActive();
        IsPlaying = false;
        SetButtonsVisible(false);
        HidePanel();
    }

    // ── Coroutines ───────────────────────────────────────────────────────

    private IEnumerator ShowLineRoutine(string line, float duration)
    {
        IsPlaying = true;
        SetText(line);
        SetButtonsVisible(false);
        ShowPanel();

        yield return new WaitForSeconds(duration);

        HidePanel();
        IsPlaying = false;
        _activeSequence = null;
    }

    private IEnumerator OrderSequenceRoutine(DialogueBuilder.OrderDialogue dialogue)
    {
        IsPlaying = true;
        SetButtonsVisible(false);

        SetText(dialogue.order);
        ShowPanel();
        yield return new WaitForSeconds(lineDisplayDuration);

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

    // ── Button handlers ──────────────────────────────────────────────────

    private void HandleAccept()
    {
        HidePrompt();
        OnAccept?.Invoke();
    }

    private void HandleReject()
    {
        HidePrompt();
        OnReject?.Invoke();
    }

    private void HandleClose()
    {
        HidePrompt();
        OnClose?.Invoke();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void StopActive()
    {
        if (_activeSequence != null)
        {
            StopCoroutine(_activeSequence);
            _activeSequence = null;
        }
        IsPlaying = false;
    }

    private void SetText(string text)
    {
        if (dialogueText != null)
            dialogueText.text = text;
    }

    private void SetButtonsVisible(bool visible)
    {
        _buttonsVisible = visible;
        if (acceptButton != null) acceptButton.gameObject.SetActive(visible);
        if (rejectButton != null) rejectButton.gameObject.SetActive(visible);
        if (closeButton != null)  closeButton.gameObject.SetActive(visible);
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
