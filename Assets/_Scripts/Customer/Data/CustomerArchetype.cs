using UnityEngine;

namespace Bufe
{
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

        /// <summary>
        /// Selects a random weighted recipe from possibleOrders.
        /// </summary>
        public Order PickOrder()
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
}
