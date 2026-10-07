using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// ScriptableObject that defines an ingredient storage source - a container
    /// (e.g. a bag of potatoes, a block of cheese) from which the player can
    /// repeatedly pull individual ingredient items.
    ///
    /// Assign one of these to an IngredientStorage MonoBehaviour placed in the scene.
    /// The storage is infinite: each interaction spawns a fresh instance of ingredientPrefab.
    ///
    /// Create via: Assets → Create → KosemBufe → Ingredient Storage
    /// </summary>
    [CreateAssetMenu(fileName = "New IngredientStorage", menuName = "KosemBufe/Ingredient Storage")]
    public class IngredientStorageData : ScriptableObject
    {
        [Tooltip("Container label (e.g. 'Cheese Block').")]
        public string storageName = "Ingredient Storage";

        [Tooltip("Spawned on interact. Needs InteractableItem.")]
        public GameObject ingredientPrefab;

        [Tooltip("Optional UI icon.")]
        public Sprite icon;

        [Tooltip("Starting stock. Negative = infinite.")]
        public int defaultStock = -1;

        [Tooltip("Unit restock cost.")]
        [Min(0f)] public float unitCost = 0f;

        [Tooltip("Restock tab.")]
        public Bufe.OrderCategory category = Bufe.OrderCategory.StapleFood;
    }
}
