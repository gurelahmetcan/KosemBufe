using UnityEngine;
using TMPro;

namespace Bufe
{
    [RequireComponent(typeof(Collider))]
    public class IngredientStorage : MonoBehaviour, IAssemblyInteractable, IHoverLabel, IRestockable
    {
        [Header("Data")]
        [Tooltip("ScriptableObject describing what ingredient this storage holds.")]
        public IngredientStorageData data;

        [Header("UI (Optional)")]
        [Tooltip("World-space TextMeshPro label. If assigned it is set to data.storageName on Start.")]
        public TextMeshPro label;

        private int _stockCount;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            _stockCount = data != null ? data.defaultStock : 0;
            if (data != null && label != null)
                label.text = data.storageName;
        }

        // ── IRestockable ─────────────────────────────────────────────────────────

        public string DisplayName => data != null ? data.storageName : name;
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
                string storageName = data != null ? data.storageName : name;
                return IsInfinite ? storageName : $"{storageName} — {_stockCount} left";
            }
        }

        // ── IAssemblyInteractable ────────────────────────────────────────────────

        /// <summary>
        /// The player can grab an ingredient only when their hand is empty, the storage has a
        /// valid prefab, and it's either infinite or has stock left.
        /// </summary>
        public bool CanInteract(PlayerHand hand)
        {
            return hand.IsHandEmpty
                   && data != null
                   && data.ingredientPrefab != null
                   && (IsInfinite || _stockCount > 0);
        }

        /// <summary>
        /// Instantiates one ingredient from the configured prefab and places it
        /// directly into the player's hand via PlayerHand.PickUp.
        /// </summary>
        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            GameObject spawnedObj = Instantiate(data.ingredientPrefab, transform.position, Quaternion.identity);

            InteractableItem item = spawnedObj.GetComponent<InteractableItem>();

            if (item == null)
            {
                Debug.LogError(
                    $"[IngredientStorage] Prefab '{data.ingredientPrefab.name}' " +
                    "has no InteractableItem component. Destroying spawned object.",
                    this);
                Destroy(spawnedObj);
                return;
            }

            hand.PickUp(item);

            if (!IsInfinite) _stockCount--;
        }
    }
}
