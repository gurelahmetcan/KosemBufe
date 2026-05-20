using UnityEngine;
using TMPro;

namespace Bufe
{
    [RequireComponent(typeof(Collider))]
    public class IngredientStorage : MonoBehaviour, IAssemblyInteractable
    {
        [Header("Data")]
        [Tooltip("ScriptableObject describing what ingredient this storage holds.")]
        public IngredientStorageData data;
    
        [Header("UI (Optional)")]
        [Tooltip("World-space TextMeshPro label. If assigned it is set to data.storageName on Start.")]
        public TextMeshPro label;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (data != null && label != null)
                label.text = data.storageName;
        }

        // ── IAssemblyInteractable ────────────────────────────────────────────────

        /// <summary>
        /// The player can grab an ingredient only when:
        ///   - their hand is empty (can't stack items in-hand), and
        ///   - this storage has a valid data asset with a prefab assigned.
        /// </summary>
        public bool CanInteract(PlayerHand hand)
        {
            return hand.IsHandEmpty
                   && data != null
                   && data.ingredientPrefab != null;
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
        }
    }
}
