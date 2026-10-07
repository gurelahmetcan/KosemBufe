using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace Bufe.UI
{
    public class CustomerOrderUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Panel holding all UI.")]
        public GameObject panel;

        [Tooltip("Dialogue and recipe text.")]
        public TextMeshProUGUI dialogueText;

        [Tooltip("Accept button.")]
        public Button acceptButton;

        [Tooltip("Reject button (customer leaves).")]
        public Button rejectButton;

        [Tooltip("Close without deciding.")]
        public Button closeButton;

        [Header("Faction Choice Mode")]
        [Tooltip("Parent for faction choice buttons.")]
        public Transform choicesContainer;

        [Tooltip("Choice button prefab (Button + TMP).")]
        public GameObject choiceButtonPrefab;

        [Tooltip("Relationship bar for factions. Optional.")]
        public FactionBarUI factionBar;

        [Header("Choice Outcome Colors")]
        [Tooltip("Rich-text style for outcome hints (e.g. +₺20).")]
        public Color positiveColor = new Color(0.20f, 0.75f, 0.20f, 1f);
        public Color negativeColor = new Color(0.85f, 0.20f, 0.20f, 1f);

        [Header("Timing")]
        [Tooltip("Seconds each line stays visible.")]
        public float lineDisplayDuration = 2f;

        [Tooltip("Pause between lines.")]
        public float linePauseDuration = 0.3f;

        /// <summary>Fired when the player clicks Accept.</summary>
        public event Action OnAccept;

        /// <summary>Fired when the player clicks Reject.</summary>
        public event Action OnReject;

        /// <summary>Fired when the player clicks Close.</summary>
        public event Action OnClose;

        /// <summary>Fired when the player picks a faction interaction choice. Passes the choice index.</summary>
        public event Action<int> OnChoicePicked;

        /// <summary>Fired when the player clicks the primary button while a conversation step is showing.</summary>
        public event Action OnContinue;

        /// <summary>True while the prompt buttons (order or faction choices) are visible.</summary>
        public bool IsPromptVisible => panel != null && panel.activeSelf && (_buttonsVisible || _choiceActive);

        /// <summary>True while dialogue text is being displayed (timed).</summary>
        public bool IsPlaying { get; private set; }

        private Coroutine _activeSequence;
        private bool _buttonsVisible;
        private bool _choiceActive;
        private bool _conversationMode;
        private readonly List<GameObject> _choiceButtons = new List<GameObject>();

        // Accept button label handling (so we can retitle it per tutorial step).
        private TextMeshProUGUI _acceptLabel;
        private string          _defaultAcceptLabel;

        private void Awake()
        {
            if (acceptButton != null) acceptButton.onClick.AddListener(HandleAccept);
            if (rejectButton != null) rejectButton.onClick.AddListener(HandleReject);
            if (closeButton != null)  closeButton.onClick.AddListener(HandleClose);

            if (acceptButton != null)
            {
                _acceptLabel = acceptButton.GetComponentInChildren<TextMeshProUGUI>();
                if (_acceptLabel != null) _defaultAcceptLabel = _acceptLabel.text;
            }

            Hide();
        }

        private void SetAcceptLabel(string label)
        {
            if (_acceptLabel == null) return;
            _acceptLabel.text = string.IsNullOrEmpty(label) ? _defaultAcceptLabel : label;
        }

        // ── Dialogue API (text only, auto-hides after duration) ──────────────

        /// <summary>
        /// Plays the order sequence: order line → optional modifier line.
        /// Buttons are NOT shown during this - they appear via ShowPrompt().
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

        /// <summary>
        /// Shows a conversation step on the panel: the line plus a single primary
        /// button (Accept, retitled to <paramref name="buttonLabel"/>). The panel
        /// stays open; clicking the button fires <see cref="OnContinue"/> so the
        /// caller can advance to the next step. Used by the tutorial.
        /// </summary>
        public void ShowContinue(string line, string buttonLabel)
        {
            StopActive();
            ClearChoices();
            SetText(line);
            SetAcceptLabel(buttonLabel);

            _conversationMode = true;
            _buttonsVisible   = true;
            if (acceptButton != null) acceptButton.gameObject.SetActive(true);
            if (rejectButton != null) rejectButton.gameObject.SetActive(false);
            if (closeButton  != null) closeButton.gameObject.SetActive(false);

            ShowPanel();
        }

        // ── Prompt API (text + buttons, stays until player clicks) ──────────

        /// <summary>
        /// Shows the order prompt with recipe text and Accept/Reject/Close buttons.
        /// Stays visible until the player clicks a button. Reject/Close can be
        /// suppressed and the Accept caption overridden (e.g. the tutorial's
        /// forced-accept order).
        /// </summary>
        public void ShowPrompt(string recipe, bool showReject = true, bool showClose = true, string acceptLabel = null)
        {
            StopActive();
            SetText(recipe);
            SetAcceptLabel(acceptLabel);

            _conversationMode = false;
            _buttonsVisible   = true;
            if (acceptButton != null) acceptButton.gameObject.SetActive(true);
            if (rejectButton != null) rejectButton.gameObject.SetActive(showReject);
            if (closeButton  != null) closeButton.gameObject.SetActive(showClose);

            ShowPanel();
        }

        /// <summary>
        /// Hides the prompt buttons (panel stays if dialogue is still playing).
        /// </summary>
        public void HidePrompt()
        {
            SetButtonsVisible(false);
            ClearChoices();
            HidePanel();
        }

        // ── Faction choice prompt (dialogue + dynamic choices) ────────────────

        /// <summary>
        /// Shows a faction member's line plus dynamic choice buttons, keeping the 3D member
        /// visible in-scene. Replaces the Accept/Reject/Close buttons for this interaction.
        /// The player's pick is reported via OnChoicePicked; the caller applies effects.
        /// </summary>
        public void ShowChoicePrompt(string line, IReadOnlyList<EventChoice> choices, FactionData faction)
        {
            StopActive();
            SetButtonsVisible(false);
            ClearChoices();

            SetText(line);
            BuildChoices(choices);

            if (factionBar != null)
            {
                factionBar.gameObject.SetActive(faction != null);
                if (faction != null) factionBar.SetFaction(faction);
            }

            _choiceActive = true;
            ShowPanel();
        }

        private void BuildChoices(IReadOnlyList<EventChoice> choices)
        {
            if (choicesContainer == null || choiceButtonPrefab == null || choices == null) return;

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i; // capture for lambda
                var go  = Instantiate(choiceButtonPrefab, choicesContainer);
                var btn = go.GetComponent<Button>();
                var lbl = go.GetComponentInChildren<TextMeshProUGUI>();

                if (lbl != null) lbl.text = ChoiceHintFormatter.BuildLabel(choices[i], positiveColor, negativeColor);
                if (btn != null) btn.onClick.AddListener(() => HandleChoicePicked(index));

                _choiceButtons.Add(go);
            }
        }

        private void HandleChoicePicked(int index)
        {
            // Lock buttons against double-clicks; the caller drives what happens next.
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            SetChoiceButtonsInteractable(false);
            OnChoicePicked?.Invoke(index);
        }

        private void ClearChoices()
        {
            for (int i = 0; i < _choiceButtons.Count; i++)
                if (_choiceButtons[i] != null) Destroy(_choiceButtons[i]);
            _choiceButtons.Clear();
            _choiceActive = false;

            if (factionBar != null) factionBar.gameObject.SetActive(false);
        }

        private void SetChoiceButtonsInteractable(bool value)
        {
            foreach (var go in _choiceButtons)
            {
                if (go == null) continue;
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = value;
            }
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
            ClearChoices();
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

            HidePanel();
            IsPlaying = false;
            _activeSequence = null;
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void HandleAccept()
        {
            // During a conversation the primary button just advances - the panel
            // stays open and the caller decides what to show next.
            if (_conversationMode)
            {
                OnContinue?.Invoke();
                return;
            }

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
            if (!visible) _conversationMode = false;
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
}
