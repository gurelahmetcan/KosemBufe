using System;
using System.Collections;
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

    [Header("Visuals")]
    [Tooltip("Animator holding the shutter open clip. Played forward to open, backward to close.")]
    public Animator animator;

    [Tooltip("Name of the state that holds the open animation clip (Loop Time must be OFF).")]
    public string animStateName = "ShutterAnim";

    [Tooltip("Seconds for the shutter to lower (close). Match your open clip length for a symmetric reverse.")]
    public float closeDuration = 1.2f;

    private Coroutine _closeRoutine;

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

        dayManager.OpenShutter();   // → phase Open → OnPhaseChanged → OpenAnim() raises the shutter

        Debug.Log("[Shutter] Büfe opened!");
    }

    /// <summary>Plays the open clip forward (0 → 1).</summary>
    public void OpenAnim()
    {
        if (animator == null) return;
        if (_closeRoutine != null) { StopCoroutine(_closeRoutine); _closeRoutine = null; }

        animator.speed = 1f;
        animator.Play(animStateName, 0, 0f);
    }

    /// <summary>
    /// Lowers the shutter — the reverse of opening. Unity forbids a negative Animator.speed
    /// at runtime, so instead of playing the clip backwards we scrub its normalized time
    /// from 1 → 0 ourselves (speed held at 0).
    /// </summary>
    public void CloseAnim()
    {
        if (animator == null) return;

        if (AudioManager.Instance)
            AudioManager.Instance.PlaySfx(Sfx.Shutter);

        if (_closeRoutine != null) StopCoroutine(_closeRoutine);
        _closeRoutine = StartCoroutine(ScrubClose());
    }

    private IEnumerator ScrubClose()
    {
        animator.speed = 0f;                     // we drive the playhead manually (never negative)
        float dur = Mathf.Max(0.0001f, closeDuration);
        float t   = 1f;

        while (t > 0f)
        {
            t -= Time.deltaTime / dur;
            animator.Play(animStateName, 0, Mathf.Clamp01(t));
            animator.Update(0f);                 // force it to sample the new pose this frame
            yield return null;
        }

        animator.Play(animStateName, 0, 0f);     // settle fully closed
        animator.Update(0f);
        _closeRoutine = null;
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
