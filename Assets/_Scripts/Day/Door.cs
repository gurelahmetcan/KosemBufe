using Bufe;
using UnityEngine;

/// <summary>
/// The door inside the büfe. Clicking it ends the day after all customers are served.
/// Attach to the door GameObject (needs a Collider for raycasting).
/// </summary>
public class Door : MonoBehaviour, IAssemblyInteractable
{
    [Header("References")]
    [Tooltip("DayManager that controls the day cycle.")]
    public DayManager dayManager;

    public bool CanInteract(PlayerHand hand)
    {
        return dayManager != null && dayManager.CanEndDay;
    }

    public void Interact(PlayerHand hand)
    {
        if (!CanInteract(hand)) return;
        Debug.Log("[Door] Day ended. Going home.");
        dayManager.EndDay();
    }
}
