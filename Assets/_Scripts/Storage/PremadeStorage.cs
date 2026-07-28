using UnityEngine;
using TMPro;

namespace Bufe
{
    /// <summary>
    /// Scene container that dispenses a premade pickup into the player's hand, with finite stock.
    /// Mirrors IngredientStorage. Restocked via the phone order screen (IRestockable).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PremadeStorage : MonoBehaviour, IAssemblyInteractable, IHoverLabel, IRestockable
    {
        [Header("Data")]
        public PremadeStorageData data;

        [Header("UI (Optional)")]
        [Tooltip("World-space label. If assigned it is set to the item name on Start.")]
        public TextMeshPro label;

        private int _stockCount;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            _stockCount = data != null ? data.defaultStock : 0;
            if (data != null && label != null)
                label.text = data.itemName;
        }

        // ── IRestockable ─────────────────────────────────────────────────────────

        public string DisplayName => data != null ? data.itemName : name;
        public Sprite Icon        => data != null ? data.icon : null;
        public float  UnitCost    => data != null ? data.unitCost : 0f;
        public int    StockCount  => _stockCount;
        public bool   IsInfinite  => data != null && data.defaultStock < 0;
        public OrderCategory Category => data != null ? data.category : OrderCategory.StapleFood;

        public void AddStock(int amount)
        {
            if (IsInfinite || amount <= 0) return;
            _stockCount += amount;
        }

        // ── IHoverLabel ──────────────────────────────────────────────────────────

        public string HoverLabel
        {
            get
            {
                string itemName = data != null ? data.itemName : name;
                return IsInfinite ? itemName : $"{itemName} — {_stockCount} left";
            }
        }

        // ── IAssemblyInteractable ────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand)
        {
            return hand.IsHandEmpty
                   && data != null
                   && data.itemPrefab != null
                   && (IsInfinite || _stockCount > 0);
        }

        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            GameObject spawnedObj = Instantiate(data.itemPrefab, transform.position, Quaternion.identity);
            DailySpawn.Tag(spawnedObj);

            InteractableItem item = spawnedObj.GetComponent<InteractableItem>();
            if (item == null)
            {
                Debug.LogError(
                    $"[PremadeStorage] Prefab '{data.itemPrefab.name}' has no InteractableItem component. " +
                    "Destroying spawned object.", this);
                Destroy(spawnedObj);
                return;
            }

            hand.PickUp(item);

            if (!IsInfinite) _stockCount--;
        }
    }
}
