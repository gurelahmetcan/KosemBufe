using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    [Serializable]
    public class EventEffect
    {
        [Tooltip("Affected family member. Null = none.")]
        public FamilyMemberData targetMember;

        [Tooltip("Family relationship change (0-10 scale).")]
        public int relationshipDelta;

        [Tooltip("Forced health change. None = no change.")]
        public FamilyHealthChange healthChange = FamilyHealthChange.None;

        [Header("Faction (demo)")]
        [Tooltip("Affected faction. Null = none.")]
        public FactionData targetFaction;

        [Tooltip("Faction relationship change (0-10 scale).")]
        public int factionDelta;

        [Tooltip("Money change. Negative = cost.")]
        public float moneyDelta;

        [Tooltip("Item spawned at the grant point (büfe only). Null = none.")]
        public InteractableItem grantedItemPrefab;
    }

    [Serializable]
    public class EventChoice
    {
        [Tooltip("Button text.")]
        public string label;

        [Tooltip("Demo-end recap line. Empty = not logged.")]
        public string summaryLine;

        [Tooltip("Effects fired on pick.")]
        public List<EventEffect> effects = new List<EventEffect>();

        [Tooltip("QuestItem id destroyed on pick. Empty = none.")]
        public string consumeQuestId;

        [Tooltip("Follow-up event on a later day (night or büfe).")]
        public GameEvent followUpEvent;

        [Tooltip("Days until the follow-up (min 1).")]
        [Min(1)] public int followUpDelay = 1;
    }

    [CreateAssetMenu(fileName = "FamilyEvent", menuName = "Kosem Bufe/Family Event")]
    public class FamilyEvent : GameEvent
    {
        public override EventSurface Surface => EventSurface.Night;

        [Header("Dialogue")]
        [Tooltip("Speaker. Null = narrator.")]
        public FamilyMemberData speaker;

        [TextArea(3, 6)]
        public string eventText;

        [Header("Choices")]
        public List<EventChoice> choices = new List<EventChoice>();

        [Header("Scheduling")]
        [Tooltip("0 = random pool. Else fires on this day.")]
        public int scheduledDay = 0;

        [Header("Random Pool Settings")]
        [Tooltip("Earliest day. Ignored if scheduled.")]
        [Min(1)] public int minDay = 1;

        [Tooltip("Daily fire chance. Ignored if scheduled.")]
        [Range(0f, 1f)] public float triggerChance = 0.3f;

        // Runtime only - resets every Play session. Not saved to disk.
        [NonSerialized] public bool HasTriggered;
    }
}
