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

    [Tooltip("Animator holding the shutter open clip. Played forward to open, backward to close.")]
    public Animator animator;

    [Tooltip("Name of the state that holds the open animation clip (Loop Time must be OFF).")]
    public string animStateName = "ShutterAnim";

    private void Awake()
    {
        // Start fully closed (first frame of the open clip), frozen until a phase change.
        if (animator != null)
        {
            animator.Play(animStateName, 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
        }
    }

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

        if(AudioManager.Instance)
            AudioManager.Instance.PlaySfx(Sfx.Shutter);

        dayManager.OpenShutter();

        if (shutterVisual != null)
            shutterVisual.SetActive(false);
        
        Debug.Log("[Shutter] Büfe opened!");
    }

    /// <summary>Plays the open clip forward (0 → 1).</summary>
    public void OpenAnim()
    {
        if (animator == null) return;
        animator.speed = 1f;
        animator.Play(animStateName, 0, 0f);
    }

    /// <summary>Plays the open clip backward (1 → 0) — the exact reverse of opening.</summary>
    public void CloseAnim()
    {
        if (animator == null) return;
        animator.speed = -1f;
        animator.Play(animStateName, 0, 1f);

        if (AudioManager.Instance)
            AudioManager.Instance.PlaySfx(Sfx.Shutter);
    }

    private void OnPhaseChanged(DayManager.DayPhase obj)
    {
        switch (obj)
        {
            case DayManager.DayPhase.PreOpen:
                break;
            case DayManager.DayPhase.Open:
                OpenAnim();
                break;
            case DayManager.DayPhase.Closing:
                break;
            case DayManager.DayPhase.EndOfDay:
                CloseAnim();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }
    }
}
