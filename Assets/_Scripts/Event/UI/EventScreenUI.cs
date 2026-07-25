using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    public class EventScreenUI : MonoBehaviour
    {
        [Header("Screen Root")]
        public GameObject screenRoot;

        [Header("Speaker")]
        public Image           speakerPortrait;
        public TextMeshProUGUI speakerNameText;

        [Header("Dialogue")]
        public TextMeshProUGUI dialogueText;

        [Header("Speaker Relationship")]
        [Tooltip("Segmented bar showing the speaker's relationship level.")]
        public RelationshipBarUI relationshipBar;
        [Tooltip("Pause (seconds) after a choice changes the speaker's relationship, so the player sees it.")]
        [Min(0f)] public float postChoiceDelay = 0.6f;

        [Header("Choices")]
        [Tooltip("Parent container for choice buttons.")]
        public Transform  choicesContainer;
        public GameObject choiceButtonPrefab;

        [Header("Effect Hint Colors")]
        [Tooltip("Applied to a choice's effect hint (e.g. -₺200) via TMP rich text.")]
        public Color positiveColor = new Color(0.20f, 0.75f, 0.20f, 1f);
        public Color negativeColor = new Color(0.85f, 0.20f, 0.20f, 1f);

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Raised when the player selects a choice. (event, choiceIndex)</summary>
        public event Action<FamilyEvent, int> OnChoiceSelected;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private FamilyEvent         _current;
        private RelationshipManager _relManager;
        private bool                _inputLocked;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (screenRoot != null) screenRoot.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void Show(FamilyEvent evt, RelationshipManager relManager, FamilyManager familyManager)
        {
            _current     = evt;
            _relManager  = relManager;
            _inputLocked = false;

            if (screenRoot != null) screenRoot.SetActive(true);

            PopulateSpeaker(evt);
            UpdateSpeakerBar(SpeakerValue());
            PopulateChoices(evt);

            if (dialogueText != null) dialogueText.text = evt.eventText;
        }

        public void Hide()
        {
            if (screenRoot != null) screenRoot.SetActive(false);
        }

        // ── Population ────────────────────────────────────────────────────────────

        private void PopulateSpeaker(FamilyEvent evt)
        {
            if (speakerPortrait != null)
            {
                bool has = evt.speaker != null && evt.speaker.portrait != null;
                speakerPortrait.gameObject.SetActive(has);
                if (has) speakerPortrait.sprite = evt.speaker.portrait;
            }

            if (speakerNameText != null)
                speakerNameText.text = evt.speaker != null ? evt.speaker.memberName.ToUpper() : string.Empty;
        }

        /// <summary>Current speaker's relationship value, or 0 if untracked/none.</summary>
        private int SpeakerValue()
        {
            if (_relManager == null || _current?.speaker == null) return 0;
            return _relManager.GetValue(_current.speaker);
        }

        /// <summary>Refreshes the speaker bar to the given value (hidden when there's no speaker).</summary>
        private void UpdateSpeakerBar(int value)
        {
            if (relationshipBar == null) return;

            var speaker = _current?.speaker;
            if (speaker == null || _relManager == null)
            {
                relationshipBar.gameObject.SetActive(false);
                return;
            }

            relationshipBar.gameObject.SetActive(true);
            relationshipBar.SetValue(value, _relManager.GetLevel(speaker));
        }

        private void PopulateChoices(FamilyEvent evt)
        {
            foreach (Transform child in choicesContainer) Destroy(child.gameObject);

            for (int i = 0; i < evt.choices.Count; i++)
            {
                int index  = i; // capture for lambda
                var choice = evt.choices[i];

                var go  = Instantiate(choiceButtonPrefab, choicesContainer);
                var btn = go.GetComponent<Button>();
                var lbl = go.GetComponentInChildren<TextMeshProUGUI>();

                if (lbl != null) lbl.text = BuildChoiceLabel(choice);
                if (btn != null) btn.onClick.AddListener(() => HandleChoiceClicked(index));
            }
        }

        // ── Choice handling ───────────────────────────────────────────────────────

        private void HandleChoiceClicked(int choiceIndex)
        {
            if (_inputLocked) return;
            _inputLocked = true;

            SetChoiceButtonsInteractable(false);

            int oldValue = SpeakerValue();

            // Notify manager — this applies money and relationship changes immediately
            OnChoiceSelected?.Invoke(_current, choiceIndex);

            int newValue = SpeakerValue();
            UpdateSpeakerBar(newValue);

            // Give the player a beat to read the change when the speaker's bar moved, then advance
            if (newValue != oldValue && postChoiceDelay > 0f)
                Invoke(nameof(FinishChoice), postChoiceDelay);
            else
                FinishChoice();
        }

        private void FinishChoice()
        {
            EventScreenManager.Instance?.NotifyAnimationComplete();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private string BuildChoiceLabel(EventChoice choice)
        {
            var sb = new StringBuilder(choice.label);

            bool firstHint = true;
            foreach (var effect in choice.effects)
            {
                string hint = BuildEffectHint(effect);
                if (string.IsNullOrEmpty(hint)) continue;

                sb.Append(firstHint ? "\n" : " ");   // results drop to a new line under the choice text
                sb.Append(hint);
                firstHint = false;
            }

            return sb.ToString();
        }

        private string BuildEffectHint(EventEffect effect)
        {
            var parts = new List<string>();

            if (effect.moneyDelta != 0f)
            {
                string money = $"{(effect.moneyDelta > 0f ? "+" : "-")}₺{Mathf.Abs(effect.moneyDelta):F0}";
                parts.Add(Colorize($"({money})", effect.moneyDelta > 0f));
            }

            if (effect.targetMember != null && effect.relationshipDelta != 0)
            {
                int r = effect.relationshipDelta;
                string rel = $"{(r > 0 ? "+" : "-")}{Mathf.Abs(r)} {effect.targetMember.memberName}";
                parts.Add(Colorize($"({rel})", r > 0));
            }

            return string.Join(" ", parts);
        }

        /// <summary>Wraps text in a TMP color tag — positiveColor when good, negativeColor when bad.</summary>
        private string Colorize(string text, bool positive)
        {
            Color c = positive ? positiveColor : negativeColor;
            return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";
        }

        private void SetChoiceButtonsInteractable(bool value)
        {
            foreach (Transform child in choicesContainer)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) btn.interactable = value;
            }
        }
    }
}
