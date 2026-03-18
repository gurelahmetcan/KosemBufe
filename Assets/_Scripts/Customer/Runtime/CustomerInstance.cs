using UnityEngine;

/// <summary>
/// A resolved snapshot of a single customer visit.
/// Created by CustomerFactory from a CustomerArchetype.
/// This is a plain C# class — lightweight, no MonoBehaviour overhead.
/// </summary>
public class CustomerInstance
{
    public CustomerArchetype Archetype   { get; }
    public CustomerMood      Mood        { get; }
    public RecipeData        Order       { get; }
    public OrderModifier     Modifier    { get; }  // Can be null
    public Sprite            Sprite      { get; }

    public CustomerInstance(
        CustomerArchetype archetype,
        CustomerMood mood,
        RecipeData order,
        OrderModifier modifier,
        Sprite sprite)
    {
        Archetype = archetype;
        Mood      = mood;
        Order     = order;
        Modifier  = modifier;
        Sprite    = sprite;
    }

    /// <summary>
    /// Returns the patience duration for this customer,
    /// factoring in the archetype's patience multiplier.
    /// </summary>
    public float GetPatienceDuration(float baseDuration)
    {
        return baseDuration * Archetype.patienceMultiplier;
    }

    public bool HasModifier => Modifier != null;
}
