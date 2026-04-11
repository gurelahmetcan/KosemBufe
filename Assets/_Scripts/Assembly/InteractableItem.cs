using UnityEngine;

/// <summary>
/// Base class for any physical item in the game world that the player
/// can pick up, hold, and place on stations or prep surfaces.
///
/// Subclass this for specific item types (e.g. Ingredient, FinalizedStack).
/// Editor-placed items should have a Collider for raycast detection.
/// Runtime-created items (FinalizedStack) add their own collider in code.
/// </summary>
public class InteractableItem : MonoBehaviour
{
    [Tooltip("Display name shown in the hand UI (e.g. 'Sucuk', 'Ekmek').")]
    public string itemName = "Item";

    [Tooltip("Which cooking station processes this item supports.")]
    public ProcessType supportedProcesses = ProcessType.None;

    /// <summary>
    /// Local rotation authored on the prefab / scene object at Awake.
    /// Stations restore this when placing the item so the original
    /// orientation is preserved through pickup → release cycles.
    /// </summary>
    public Quaternion OriginalLocalRotation { get; private set; }

    /// <summary>
    /// Local scale authored on the prefab / scene object at Awake.
    /// Stations restore this when placing the item so the original
    /// size is preserved through pickup → release cycles.
    /// </summary>
    public Vector3 OriginalLocalScale { get; private set; }

    protected virtual void Awake()
    {
        OriginalLocalRotation = transform.localRotation;
        OriginalLocalScale    = transform.localScale;
    }

    /// <summary>
    /// Notification called after the player picks this item up.
    /// Physics and parenting are handled by PlayerHand — use this
    /// for custom reactions (sounds, VFX, etc.).
    /// </summary>
    public virtual void OnPickedUp() { }

    /// <summary>
    /// Notification called after the player drops this item.
    /// Physics and unparenting are handled by PlayerHand — use this
    /// for custom reactions (sounds, VFX, etc.).
    /// </summary>
    public virtual void OnDropped() { }
}
