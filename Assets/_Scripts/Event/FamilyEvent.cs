using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    [Serializable]
    public class EventEffect
    {
        [Tooltip("Which member is affected. Null = money-only effect.")]
        public FamilyMemberData targetMember;

        [Tooltip("Relationship change (0–10 scale). Positive = gain, negative = loss.")]
        public int relationshipDelta;

        [Tooltip("Money change. Negative = costs player money, positive = gives money.")]
        public float moneyDelta;
    }

    [Serializable]
    public class EventChoice
    {
        [Tooltip("Button text shown to the player.")]
        public string label;

        [Tooltip("All effects that fire when this choice is selected.")]
        public List<EventEffect> effects = new List<EventEffect>();

        [Tooltip("Optional event to schedule after this choice. Fires on a future day, not immediately.")]
        public FamilyEvent followUpEvent;

        [Tooltip("How many days until the follow-up event fires (minimum 1 = next day).")]
        [Min(1)] public int followUpDelay = 1;
    }

    [CreateAssetMenu(fileName = "FamilyEvent", menuName = "Kosem Bufe/Family Event")]
    public class FamilyEvent : ScriptableObject
    {
        [Header("Dialogue")]
        [Tooltip("Who is speaking. Null = narrator (no portrait shown).")]
        public FamilyMemberData speaker;

        [TextArea(3, 6)]
        public string eventText;

        [Header("Choices")]
        public List<EventChoice> choices = new List<EventChoice>();

        [Header("Scheduling")]
        [Tooltip("0 = random pool. Greater than 0 = fires on exactly this day number.")]
        public int scheduledDay = 0;

        [Header("Random Pool Settings")]
        [Tooltip("Ignored when scheduledDay > 0. Don't roll before this day.")]
        [Min(1)] public int minDay = 1;

        [Tooltip("Ignored when scheduledDay > 0. Probability this event fires on any eligible day.")]
        [Range(0f, 1f)] public float triggerChance = 0.3f;

        // Runtime only — resets every Play session. Not saved to disk.
        [NonSerialized] public bool HasTriggered;
    }
}
