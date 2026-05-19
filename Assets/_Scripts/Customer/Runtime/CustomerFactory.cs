using UnityEngine;

/// <summary>
/// Builds CustomerInstances from a pool of archetypes.
/// This is the single entry point for customer creation — when Recurring
/// and Special customers are added later, this class gains new methods
/// (e.g. CreateRecurringCustomer, CreateSpecialCustomer) without
/// disrupting the normal customer flow.
/// </summary>

namespace Bufe
{
    public class CustomerFactory : MonoBehaviour
    {
        [Header("Normal Customer Pool")]
        [Tooltip("All archetypes available for normal/everyday customers.")]
        public CustomerArchetype[] archetypePool;

        /// <summary>
        /// Creates a fully resolved normal customer instance.
        /// Picks a random archetype, then resolves mood, order, modifier, and sprite.
        /// Returns null if the pool is empty or the archetype has no valid orders/moods.
        /// </summary>
        public CustomerInstance CreateNormalCustomer()
        {
            if (archetypePool == null || archetypePool.Length == 0)
            {
                Debug.LogError("[CustomerFactory] Archetype pool is empty!");
                return null;
            }

            CustomerArchetype archetype = archetypePool[Random.Range(0, archetypePool.Length)];
            return CreateFromArchetype(archetype);
        }

        /// <summary>
        /// Creates a customer instance from a specific archetype.
        /// Useful for when you want to force a particular archetype (testing, events).
        /// </summary>
        public CustomerInstance CreateFromArchetype(CustomerArchetype archetype)
        {
            if (archetype == null)
            {
                Debug.LogError("[CustomerFactory] Null archetype provided!");
                return null;
            }

            CustomerMood mood = archetype.PickMood();
            if (mood == null)
            {
                Debug.LogError($"[CustomerFactory] Archetype '{archetype.archetypeName}' has no moods!");
                return null;
            }

            OrderItem order = archetype.PickOrder();
            if (order == null)
            {
                Debug.LogError($"[CustomerFactory] Archetype '{archetype.archetypeName}' has no orders!");
                return null;
            }
        
            Sprite sprite = archetype.PickSprite();

            return new CustomerInstance(archetype, mood, order, sprite);
        }
    }

}
