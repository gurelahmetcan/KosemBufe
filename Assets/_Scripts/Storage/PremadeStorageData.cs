using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Defines a premade-item storage source — a shelf/fridge slot that dispenses a ready-made
    /// pickup (Chips, Ayran, Cola, Cigarettes). Unlike ingredient storages these are typically
    /// finite and restocked via the phone order screen.
    ///
    /// Create via: Assets → Create → Kosem Bufe → Premade Storage
    /// </summary>
    [CreateAssetMenu(fileName = "New PremadeStorage", menuName = "Kosem Bufe/Premade Storage")]
    public class PremadeStorageData : ScriptableObject
    {
        [Tooltip("Display name shown on the storage label and order screen.")]
        public string itemName = "Premade Item";

        [Tooltip("The premade pickup prefab spawned on interact. Must have an InteractableItem component.")]
        public GameObject itemPrefab;

        [Tooltip("Starting stock. Negative = infinite (never depletes, not orderable).")]
        public int defaultStock = 10;

        [Tooltip("Cost per unit when reordering from the phone.")]
        [Min(0f)] public float unitCost = 0f;

        [Tooltip("Icon used in the order screen row.")]
        public Sprite icon;

        [Tooltip("Which tab this item appears under on the phone order screen.")]
        public OrderCategory category = OrderCategory.StapleFood;
    }
}
