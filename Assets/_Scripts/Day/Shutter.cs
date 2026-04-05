using UnityEngine;

/// <summary>
/// The shutter in front of the büfe. Clicking it opens the shop and starts the day.
/// Attach to the shutter GameObject (needs a Collider for raycasting).
///
/// Only interactable during DayPhase.PreOpen — once the shutter is opened it
/// stays open for the rest of the day.
/// </summary>
public class Shutter : MonoBehaviour, IInteractable
{
    [Header("References")]
    [Tooltip("DayManager that controls the day cycle.")]
    public DayManager dayManager;

    [Header("Visuals (optional)")]
    [Tooltip("Object to hide when the shutter opens (e.g. the shutter mesh/sprite).")]
    public GameObject shutterVisual;

    [Tooltip("Optional animation/sound trigger on open.")]
    public Animator animator;

    private static readonly int OpenHash = Animator.StringToHash("Open");

    public bool CanInteract(InHandController hand)
    {
        return dayManager != null && dayManager.CurrentPhase == DayManager.DayPhase.PreOpen;
    }

    public void Interact(InHandController hand)
    {
        if (!CanInteract(hand)) return;

        dayManager.OpenShutter();

        // Hide or animate the shutter visual
        if (shutterVisual != null)
            shutterVisual.SetActive(false);

        if (animator != null)
            animator.SetTrigger(OpenHash);

        Debug.Log("[Shutter] Büfe opened!");
    }
}
