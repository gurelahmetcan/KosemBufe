using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    [Serializable]
    public class EventEffect
    {
        [Tooltip("Which family member is affected. Null = no family effect.")]
        public FamilyMemberData targetMember;

        [Tooltip("Family relationship change (0–10 scale). Positive = gain, negative = loss.")]
        public int relationshipDelta;

        [Header("Faction (demo)")]
        [Tooltip("Which faction is affected. Null = no faction effect.")]
        public FactionData targetFaction;

        [Tooltip("Faction relationship change (0–10 scale). Positive = gain, negative = loss.")]
        public int factionDelta;

        [Tooltip("Money change. Negative = costs player money, positive = gives money.")]
        public float moneyDelta;

        [Tooltip("Item handed to the player. Spawned at OrderManager's item grant point (büfe " +
                 "interactions only). Null = no item. The player picks it up like any item.")]
        public InteractableItem grantedItemPrefab;
    }

    [Serializable]
    public class EventChoice
    {
        [Tooltip("Button text shown to the player.")]
        public string label;

        [Tooltip("All effects that fire when this choice is selected.")]
        public List<EventEffect> effects = new List<EventEffect>();

        [Tooltip("Optional: consume a quest item when this choice is picked. Any item in the " +
                 "world with a matching QuestItem.questId is destroyed (e.g. 'taxi_box' when " +
                 "handing the package to the police). Empty = no item consumed.")]
        public string consumeQuestId;

        [Tooltip("Optional event to schedule after this choice. Fires on a future day, not " +
                 "immediately. Can be a night event OR a day büfe interaction — the scheduler routes it.")]
        public GameEvent followUpEvent;

        [Tooltip("How many days until the follow-up event fires (minimum 1 = next day).")]
        [Min(1)] public int followUpDelay = 1;
    }

    [CreateAssetMenu(fileName = "FamilyEvent", menuName = "Kosem Bufe/Family Event")]
    public class FamilyEvent : GameEvent
    {
        public override EventSurface Surface => EventSurface.Night;

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
