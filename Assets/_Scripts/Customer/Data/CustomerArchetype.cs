using UnityEngine;

/// <summary>
/// Defines a normal customer archetype — their identity template.
/// Designers create one per visual character (e.g. "Tired Doctor", "Cheerful Student").
///
/// The system picks a mood from possibleMoods at spawn time,
/// then uses that mood's line pools for all dialogue.
///
/// Create via: Assets → Create → KosemBufe → Customer Archetype
/// </summary>
[CreateAssetMenu(fileName = "NewArchetype", menuName = "KosemBufe/Customer Archetype")]
public class CustomerArchetype : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name for this archetype (e.g. 'Doctor', 'Student').")]
    public string archetypeName;

    [Tooltip("Possible sprites for this archetype. One is picked at random per visit.")]
    public Sprite[] possibleSprites;

    [Header("Behaviour")]
    [Tooltip("Moods this archetype can spawn with. One is selected per visit.")]
    public CustomerMood[] possibleMoods;

    [Tooltip("What this archetype might order, with relative weights.")]
    public WeightedRecipe[] possibleOrders;

    [Header("Tuning")]
    [Tooltip("Multiplier on base patience duration (< 1 = impatient, > 1 = patient).")]
    [Min(0.1f)]
    public float patienceMultiplier = 1f;

    /// <summary>
    /// Selects a random weighted recipe from possibleOrders.
    /// </summary>
    public RecipeData PickOrder()
    {
        if (possibleOrders == null || possibleOrders.Length == 0) return null;

        int totalWeight = 0;
        for (int i = 0; i < possibleOrders.Length; i++)
            totalWeight += possibleOrders[i].weight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < possibleOrders.Length; i++)
        {
            cumulative += possibleOrders[i].weight;
            if (roll < cumulative)
                return possibleOrders[i].recipe;
        }

        return possibleOrders[0].recipe;
    }

    /// <summary>
    /// Selects a random mood from possibleMoods.
    /// </summary>
    public CustomerMood PickMood()
    {
        if (possibleMoods == null || possibleMoods.Length == 0) return null;
        return possibleMoods[Random.Range(0, possibleMoods.Length)];
    }

    /// <summary>
    /// Selects a random sprite from possibleSprites.
    /// </summary>
    public Sprite PickSprite()
    {
        if (possibleSprites == null || possibleSprites.Length == 0) return null;
        return possibleSprites[Random.Range(0, possibleSprites.Length)];
    }
}
