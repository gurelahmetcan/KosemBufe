using UnityEngine;

namespace Bufe
{
    public class CustomerInstance
    {
        public CustomerArchetype Archetype   { get; }
        public CustomerMood      Mood        { get; }
        public Order        Order       { get; }
        public Sprite            Sprite      { get; }

        public CustomerInstance(
            CustomerArchetype archetype,
            CustomerMood mood,
            Order order,
            Sprite sprite)
        {
            Archetype = archetype;
            Mood      = mood;
            Order     = order;
            Sprite    = sprite;
        }
    }
}