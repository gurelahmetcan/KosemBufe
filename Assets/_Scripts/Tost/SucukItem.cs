using UnityEngine;

/// <summary>
/// Add to the whole Sucuk prefab (alongside its Ingredient component).
///
/// When the player holds the knife and left-clicks the sucuk, it is destroyed
/// and replaced by a configurable number of SucukSlice objects stacked at
/// the same world position. The player must put the knife down first to
/// pick up slices one by one.
/// </summary>
namespace Bufe
{
    [RequireComponent(typeof(Ingredient))]
    public class SucukItem : MonoBehaviour, IAssemblyInteractable
    {
        [Tooltip("Prefab spawned for each slice. Must have an Ingredient component.")]
        public GameObject sucukSlicePrefab;

        [Tooltip("How many slices are produced when the sucuk is cut.")]
        public int sliceCount = 4;

        [Tooltip("Vertical offset between stacked slice objects after spawning.")]
        public float sliceStackOffset = 0.015f;

        // ── IAssemblyInteractable ─────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand)
        {
            return hand.HeldItem is KnifeItem;
        }

        public void Interact(PlayerHand hand)
        {
            if (sucukSlicePrefab == null)
            {
                Debug.LogWarning("[SucukItem] sucukSlicePrefab is not assigned.");
                return;
            }

            Vector3    spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation;

            for (int i = 0; i < sliceCount; i++)
            {
                Vector3 pos = spawnPos + Vector3.up * (sliceStackOffset * i);
                Instantiate(sucukSlicePrefab, pos, spawnRot);
            }

            Debug.Log($"[SucukItem] Sliced into {sliceCount} pieces.");
            Destroy(gameObject);
        }
    }
}
