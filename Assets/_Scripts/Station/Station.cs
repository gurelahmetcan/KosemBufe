using UnityEngine;

/// <summary>
/// Abstract base class for all cooking/processing stations.
/// Each station implements its own interaction rules and logic.
/// Attach a Collider alongside any Station subclass for raycast detection.
/// </summary>
public abstract class Station : MonoBehaviour, IInteractable
{
    public abstract bool CanInteract(InHandController hand);
    public abstract void Interact(InHandController hand);
}
