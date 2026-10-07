using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// One possible look for an archetype: a body prefab plus the material variants that
    /// fit that specific model. Keeping them paired guarantees a texture is never applied
    /// to the wrong body (e.g. a female texture on the male mesh).
    /// </summary>
    [System.Serializable]
    public class CustomerBodyOption
    {
        [Tooltip("Body prefab (full Customer setup).")]
        public Customer prefab;

        [Tooltip("Materials for this prefab. Empty = prefab default.")]
        public Material[] materialVariants;
    }

    [CreateAssetMenu(fileName = "NewArchetype", menuName = "KosemBufe/Customer Archetype")]
    public class CustomerArchetype : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name (e.g. 'Doctor').")]
        public string archetypeName;

        [Tooltip("Random sprite per visit.")]
        public Sprite[] possibleSprites;

        [Header("Appearance")]
        [Tooltip("Random body per visit. Empty = generic pool.")]
        public CustomerBodyOption[] possibleBodies;

        [Header("Behaviour")]
        [Tooltip("Random mood per visit.")]
        public CustomerMood[] possibleMoods;

        [Tooltip("Possible orders, weighted.")]
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
                    return Order.FromRecipe(possibleOrders[i].recipe);
            }

            return Order.FromRecipe(possibleOrders[0].recipe);
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

        /// <summary>
        /// Selects a random body option (prefab + one of its matching materials).
        /// Returns null when this archetype has no authored bodies - the caller then
        /// falls back to the generic pool.
        /// </summary>
        public CustomerBodyOption PickBody()
        {
            if (possibleBodies == null || possibleBodies.Length == 0) return null;
            CustomerBodyOption pick = possibleBodies[Random.Range(0, possibleBodies.Length)];
            return pick != null && pick.prefab != null ? pick : null;
        }
    }
}
