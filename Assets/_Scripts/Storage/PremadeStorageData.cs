using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Defines a premade-item storage source - a shelf/fridge slot that dispenses a ready-made
    /// pickup (Chips, Ayran, Cola, Cigarettes). Unlike ingredient storages these are typically
    /// finite and restocked via the phone order screen.
    ///
    /// Create via: Assets → Create → Kosem Bufe → Premade Storage
    /// </summary>
    [CreateAssetMenu(fileName = "New PremadeStorage", menuName = "Kosem Bufe/Premade Storage")]
    public class PremadeStorageData : ScriptableObject
    {
        [Tooltip("Label and order screen name.")]
        public string itemName = "Premade Item";

        [Tooltip("Spawned on interact. Needs InteractableItem.")]
        public GameObject itemPrefab;

        [Tooltip("Starting stock. Negative = infinite.")]
        public int defaultStock = 10;

        [Tooltip("Unit restock cost.")]
        [Min(0f)] public float unitCost = 0f;

        [Tooltip("Order row icon.")]
        public Sprite icon;

        [Tooltip("Restock tab.")]
        public OrderCategory category = OrderCategory.StapleFood;
    }
}
