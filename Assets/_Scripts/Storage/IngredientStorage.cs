using UnityEngine;
using TMPro;

/// <summary>
/// A world-placed storage container (e.g. a bag of potatoes, a block of cheese)
/// that spawns one individual ingredient per player interaction.
///
/// Implements IAssemblyInteractable so PlayerHand detects and drives it via
/// the standard left-click interaction flow — no changes to PlayerHand required.
///
/// The storage is infinite: it never runs out.
/// The spawned prefab is placed in the player's hand immediately.
///
/// Setup:
///   1. Add this component to the world GameObject representing the container.
///   2. Assign an IngredientStorageData asset to the 'data' field.
///   3. Optionally assign 'spawnPoint' — a child Transform from which the
///      ingredient will be spawned (defaults to this object's position).
///   4. Optionally assign 'label' for a world-space TextMeshPro name tag.
///   5. Ensure a Collider is present on this GameObject for raycast detection.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IngredientStorage : MonoBehaviour, IAssemblyInteractable
{
    [Header("Data")]
    [Tooltip("ScriptableObject describing what ingredient this storage holds.")]
    public IngredientStorageData data;

    [Header("Spawn")]
    [Tooltip("Transform from which the ingredient is instantiated. " +
             "If null, this object's own position/rotation are used.")]
    public Transform spawnPoint;

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

        Vector3    spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject spawnedObj = Instantiate(data.ingredientPrefab, spawnPos, spawnRot);

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
