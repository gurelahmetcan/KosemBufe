using System;
using Bufe;
using UnityEngine;

/// <summary>
/// The shutter in front of the büfe. Clicking it opens the shop and starts the day.
/// Attach to the shutter GameObject (needs a Collider for raycasting).
/// </summary>
public class Shutter : MonoBehaviour, IAssemblyInteractable
{
    [Header("References")]
    [Tooltip("DayManager that controls the day cycle.")]
    public DayManager dayManager;

    [Header("Visuals (optional)")]
    [Tooltip("Object to hide when the shutter opens.")]
    public GameObject shutterVisual;

    [Tooltip("Optional animation trigger on open.")]
    public Animator animator;

    private static readonly int OpenHash = Animator.StringToHash("Open");

    private void OnEnable()
    {
        if (dayManager != null)
            dayManager.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        if (dayManager != null)
            dayManager.OnPhaseChanged -= OnPhaseChanged;
    }
    
    public bool CanInteract(PlayerHand hand)
    {
        return dayManager != null && dayManager.CurrentPhase == DayManager.DayPhase.PreOpen;
    }

    public void Interact(PlayerHand hand)
    {
        if (!CanInteract(hand)) return;

        dayManager.OpenShutter();

        if (shutterVisual != null)
            shutterVisual.SetActive(false);
        
        Debug.Log("[Shutter] Büfe opened!");
    }

    public void HandleShutterAnimation()
    {
        if (animator != null)
            animator.SetTrigger(OpenHash);
    }
    
    private void OnPhaseChanged(DayManager.DayPhase obj)
    {
        switch (obj)
        {
            case DayManager.DayPhase.PreOpen:
                break;
            case DayManager.DayPhase.Open:
                HandleShutterAnimation();
                break;
            case DayManager.DayPhase.Closing:
                break;
            case DayManager.DayPhase.EndOfDay:
                HandleShutterAnimation();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }
    }
}
