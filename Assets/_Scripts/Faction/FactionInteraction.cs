using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// What a faction member does at the büfe:
    ///   Offer   — says a line, player picks from choices (each with effects/items/follow-ups).
    ///   Collect — wants a specific item; the player hands it over like delivering an order.
    /// </summary>
    public enum InteractionKind { Offer, Collect }

    /// <summary>
    /// A scripted faction-member visit at the büfe. Presented in the extended order prompt
    /// (CustomerOrderUI) — the 3D member stays visible; NOT a screen-covering modal.
    /// Shares the EventChoice/EventEffect primitives (see FamilyEvent.cs) so any interaction
    /// can move faction/money, grant items, and schedule night consequences.
    ///
    /// DEMO: no cooking. Authored content, scripted spawn.
    ///
    /// Create via: Assets → Create → Kosem Bufe → Faction Interaction
    /// </summary>
    [CreateAssetMenu(fileName = "FactionInteraction", menuName = "Kosem Bufe/Faction Interaction")]
    public class FactionInteraction : GameEvent
    {
        public override EventSurface Surface => EventSurface.DayBufe;

        [Header("Member")]
        [Tooltip("Which faction this member belongs to — drives the inline relationship bar.")]
        public FactionData faction;

        [Tooltip("Designed member's display name (e.g. 'Taxi Driver Cemal').")]
        public string memberName = "Faction Member";

        [Tooltip("Optional portrait. The 3D model in-scene is the primary visual.")]
        public Sprite portrait;

        [Header("Interaction")]
        public InteractionKind kind = InteractionKind.Offer;

        [TextArea(2, 5)]
        [Tooltip("Offer: the line shown above the choices. Collect: the reminder line shown " +
                 "when the player clicks without the required item.")]
        public string dialogue;

        [Header("Offer — choices")]
        [Tooltip("Used when kind = Offer. Each choice's effects apply on selection (see EventChoice).")]
        public List<EventChoice> choices = new List<EventChoice>();

        [Header("Collect — item handoff")]
        [Tooltip("Used when kind = Collect. The item the player must hand over (matched like an order).")]
        public OrderItem requiredItem;

        [Tooltip("Used when kind = Collect. Effects applied once the item is handed over.")]
        public List<EventEffect> onCollectedEffects = new List<EventEffect>();
    }
}
