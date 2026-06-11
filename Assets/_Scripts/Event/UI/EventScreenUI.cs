using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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

        [Header("Members Panel")]
        [Tooltip("Parent container for EventMemberUI rows (only involved members are shown).")]
        public Transform  membersContainer;
        public GameObject memberRowPrefab;

        [Header("Choices")]
        [Tooltip("Parent container for choice buttons.")]
        public Transform  choicesContainer;
        public GameObject choiceButtonPrefab;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Raised when the player selects a choice. (event, choiceIndex)</summary>
        public event Action<FamilyEvent, int> OnChoiceSelected;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private FamilyEvent                                    _current;
        private RelationshipManager                            _relManager;
        private readonly List<(FamilyMemberData, EventMemberUI)> _memberUIs =
            new List<(FamilyMemberData, EventMemberUI)>();
        private bool _inputLocked;

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
            PopulateMembers(evt, relManager, familyManager);
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

        private void PopulateMembers(FamilyEvent evt, RelationshipManager relManager, FamilyManager familyManager)
        {
            foreach (Transform child in membersContainer) Destroy(child.gameObject);
            _memberUIs.Clear();

            foreach (var memberData in GetInvolvedMembers(evt, familyManager))
            {
                var go = Instantiate(memberRowPrefab, membersContainer);
                var ui = go.GetComponent<EventMemberUI>();
                if (ui == null) continue;

                int value = relManager != null ? relManager.GetValue(memberData) : 5;
                ui.Setup(memberData, value);
                _memberUIs.Add((memberData, ui));
            }
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

            // Notify manager — this applies money and relationship changes immediately
            OnChoiceSelected?.Invoke(_current, choiceIndex);

            // Animate affected members, then hand back to EventScreenManager
            AnimateEffects(_current.choices[choiceIndex]);
        }

        private void AnimateEffects(EventChoice choice)
        {
            var masterSeq = DOTween.Sequence();
            bool hasAnim  = false;

            foreach (var effect in choice.effects)
            {
                if (effect.targetMember == null || effect.relationshipDelta == 0) continue;

                foreach (var (data, ui) in _memberUIs)
                {
                    if (data != effect.targetMember) continue;

                    // RelationshipManager has already applied the change.
                    // Reconstruct old value: old = new - delta (pre-clamp approximation).
                    int newVal = _relManager != null ? _relManager.GetValue(data) : 5;
                    int oldVal = Mathf.Clamp(newVal - effect.relationshipDelta, 0, 10);

                    masterSeq.Join(ui.AnimateTo(newVal, oldVal));
                    hasAnim = true;
                }
            }

            if (hasAnim)
                masterSeq.OnComplete(() => EventScreenManager.Instance?.NotifyAnimationComplete());
            else
                EventScreenManager.Instance?.NotifyAnimationComplete();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private List<FamilyMemberData> GetInvolvedMembers(FamilyEvent evt, FamilyManager familyManager)
        {
            var seen   = new HashSet<FamilyMemberData>();
            var result = new List<FamilyMemberData>();

            void TryAdd(FamilyMemberData m)
            {
                if (m == null || seen.Contains(m)) return;
                foreach (var state in familyManager.States)
                    if (state.Data == m && state.IsGone) return;
                seen.Add(m);
                result.Add(m);
            }

            TryAdd(evt.speaker);
            foreach (var choice in evt.choices)
                foreach (var effect in choice.effects)
                    TryAdd(effect.targetMember);

            return result;
        }

        private string BuildChoiceLabel(EventChoice choice)
        {
            var sb = new StringBuilder(choice.label);

            bool firstHint = true;
            foreach (var effect in choice.effects)
            {
                string hint = BuildEffectHint(effect);
                if (string.IsNullOrEmpty(hint)) continue;

                sb.Append(firstHint ? "\n" : "  ");
                sb.Append(hint);
                firstHint = false;
            }

            return sb.ToString();
        }

        private string BuildEffectHint(EventEffect effect)
        {
            var parts = new List<string>();

            if (effect.moneyDelta != 0f)
                parts.Add(effect.moneyDelta > 0f
                    ? $"+{effect.moneyDelta:F0}₺"
                    : $"{effect.moneyDelta:F0}₺");

            if (effect.targetMember != null && effect.relationshipDelta != 0)
            {
                string sign = effect.relationshipDelta > 0 ? "+" : "";
                parts.Add($"{sign}{effect.relationshipDelta} {effect.targetMember.memberName}");
            }

            return string.Join("  ", parts);
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
