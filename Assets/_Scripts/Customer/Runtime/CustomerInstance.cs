using UnityEngine;

/// <summary>
/// A resolved snapshot of a single customer visit.
/// Created by CustomerFactory from a CustomerArchetype.
/// This is a plain C# class — lightweight, no MonoBehaviour overhead.
/// </summary>
namespace Bufe
{
    public class CustomerInstance
    {
        public CustomerArchetype Archetype   { get; }
        public CustomerMood      Mood        { get; }
        public OrderItem        Order       { get; }
        public Sprite            Sprite      { get; }

        public CustomerInstance(
            CustomerArchetype archetype,
            CustomerMood mood,
            OrderItem order,
            Sprite sprite)
        {
            Archetype = archetype;
            Mood      = mood;
            Order     = order;
            Sprite    = sprite;
        }
    }
}