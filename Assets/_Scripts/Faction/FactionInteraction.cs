using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// What a faction member does at the büfe:
    ///   Offer   - says a line, player picks from choices (each with effects/items/follow-ups).
    ///   Collect - wants a specific item; the player hands it over like delivering an order.
    /// </summary>
    public enum InteractionKind { Offer, Collect }

    /// <summary>
    /// A scripted faction-member visit at the büfe. Presented in the extended order prompt
    /// (CustomerOrderUI) - the 3D member stays visible; NOT a screen-covering modal.
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
        [Tooltip("Member's faction. Drives the inline bar.")]
        public FactionData faction;

        [Tooltip("Display name (e.g. 'Taxi Driver Cemal').")]
        public string memberName = "Faction Member";

        [Tooltip("Optional portrait.")]
        public Sprite portrait;

        [Header("Appearance (optional)")]
        [Tooltip("Body prefab. Empty = default customer.")]
        public Customer bodyPrefab;

        [Tooltip("Material override. Empty = prefab default.")]
        public Material appearanceMaterial;

        [Header("Interaction")]
        public InteractionKind kind = InteractionKind.Offer;

        [TextArea(2, 5)]
        [Tooltip("Offer: prompt line. Collect: reminder line.")]
        public string dialogue;

        [Header("Offer - choices")]
        [Tooltip("Offer only. Effects apply on pick.")]
        public List<EventChoice> choices = new List<EventChoice>();

        [Header("Collect - item handoff")]
        [Tooltip("Collect only. Item to hand over.")]
        public OrderItem requiredItem;

        [Tooltip("Collect only. Effects on handover.")]
        public List<EventEffect> onCollectedEffects = new List<EventEffect>();
    }
}
