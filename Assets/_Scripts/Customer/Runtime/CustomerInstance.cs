using UnityEngine;

namespace Bufe
{
    public class CustomerInstance
    {
        public CustomerArchetype Archetype   { get; }
        public CustomerMood      Mood        { get; }
        public Order        Order       { get; }
        public Sprite            Sprite      { get; }

        /// <summary>Body prefab chosen from the archetype. Null = use OrderManager's generic pool.</summary>
        public Customer BodyPrefab { get; }

        /// <summary>Material chosen for the body. Null = the prefab's own (or random) look.</summary>
        public Material BodyMaterial { get; }

        public CustomerInstance(
            CustomerArchetype archetype,
            CustomerMood mood,
            Order order,
            Sprite sprite,
            Customer bodyPrefab   = null,
            Material bodyMaterial = null)
        {
            Archetype    = archetype;
            Mood         = mood;
            Order        = order;
            Sprite       = sprite;
            BodyPrefab   = bodyPrefab;
            BodyMaterial = bodyMaterial;
        }
    }
}
