using UnityEngine;

/// <summary>
/// Base class for any physical item in the game world that the player
/// can pick up, hold, and place on stations or prep surfaces.
///
/// Subclass this for specific item types (e.g. Ingredient, FinalizedStack).
/// Attach a Collider alongside this component for raycast detection.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour
{
    [Tooltip("Display name shown in the hand UI (e.g. 'Sucuk', 'Ekmek').")]
    public string itemName = "Item";

    [Tooltip("Which cooking station processes this item supports.")]
    public ProcessType supportedProcesses = ProcessType.None;

    /// <summary>
    /// Called when the player picks this item up from the world.
    /// Override in subclasses for custom pickup behavior.
    /// </summary>
    public virtual void OnPickedUp()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when the player drops or places this item back into the world.
    /// Override in subclasses for custom drop behavior.
    /// </summary>
    public virtual void OnDropped(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }
}
