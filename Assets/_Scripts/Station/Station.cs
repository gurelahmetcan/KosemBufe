using Bufe;
using UnityEngine;

/// <summary>
/// Abstract base class for all cooking/processing stations.
/// Each station implements its own interaction rules and logic.
/// Attach a Collider alongside any Station subclass for raycast detection.
/// </summary>
public abstract class Station : MonoBehaviour, IAssemblyInteractable
{
    public abstract bool CanInteract(PlayerHand hand);
    public abstract void Interact(PlayerHand hand);
}
