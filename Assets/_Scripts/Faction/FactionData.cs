using UnityEngine;

namespace Bufe
{
    public enum FactionId { Taxi, Police, Neighborhood }

    /// <summary>
    /// A faction the player builds a shop-wide relationship with (Taxi, Police, Neighborhood).
    /// Relationships are faction-wide, not per-member — individual designed members exist for
    /// flavour/spawning but are not tracked individually.
    ///
    /// DEMO NOTE: this is a deliberately standalone type, parallel to FamilyMemberData rather
    /// than sharing a generic RelationshipTarget base. Post-demo this merges into the
    /// generalized core (see Docs/FactionSystemDesign.md §3).
    ///
    /// Create via: Assets → Create → Kosem Bufe → Faction
    /// </summary>
    [CreateAssetMenu(fileName = "Faction", menuName = "Kosem Bufe/Faction")]
    public class FactionData : ScriptableObject
    {
        [Header("Identity")]
        public FactionId id;
        public string displayName = "Faction";

        [Tooltip("Icon shown next to this faction's relationship bar.")]
        public Sprite icon;

        [Header("Starting Value")]
        [Tooltip("Relationship value (0–10) this faction begins each run with.")]
        [Range(0, 10)] public int startingRelationship = 5;
    }
}
