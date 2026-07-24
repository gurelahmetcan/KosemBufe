using Bufe;
using UnityEngine;

/// <summary>
/// The door inside the büfe. Clicking it ends the day after all customers are served.
/// Attach to the door GameObject (needs a Collider for raycasting).
///
/// The hover label changes with the day phase: while customers may still arrive it
/// shows the "not ready" text, and once the day can be ended it shows the "ready" text.
/// Both strings are editable in the Inspector.
/// </summary>
public class Door : MonoBehaviour, IAssemblyInteractable, IHoverLabel
{
    [Header("References")]
    [Tooltip("DayManager that controls the day cycle.")]
    public DayManager dayManager;

    [Header("Hover Labels")]
    [Tooltip("Shown when the day can be ended (player may leave).")]
    [SerializeField] private string readyLabel = "Go Home";

    [Tooltip("Shown while the day is still running and the door can't be used yet.")]
    [SerializeField] private string notReadyLabel = "It's not time to close yet.";

    // ── IHoverLabel ────────────────────────────────────────────────────────────

    // Property (not a field) — required by the interface. It's computed from the
    // day state at query time, so it stays in sync as the phase changes.
    public string HoverLabel => CanInteract(null) ? readyLabel : notReadyLabel;

    // ── IAssemblyInteractable ──────────────────────────────────────────────────

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
