using UnityEngine;

/// <summary>
/// Attached at runtime to each food visual sitting on the grill.
/// Provides click-to-collect (IInteractable) and F-to-flip (IFlippable)
/// for the specific slot it represents.
///
/// Created and configured by GrillStation.PlaceOnGrill().
/// </summary>
public class GrillSlotDisplay : MonoBehaviour, IInteractable, IFlippable
{
    /// <summary>The owning grill station (set by GrillStation on creation).</summary>
    public GrillStation Station { get; set; }

    /// <summary>Which slot index this display represents.</summary>
    public int SlotIndex { get; set; }

    // ── IInteractable — click to collect ─────────────────────────────────────

    public bool CanInteract(InHandController hand)
    {
        // Empty hand → collect the item from the grill
        return hand.HeldItem == null;
    }

    public void Interact(InHandController hand)
    {
        Station.CollectFromSlot(SlotIndex, hand);
    }

    // ── IFlippable — press F to flip ─────────────────────────────────────────

    public bool CanFlip()
    {
        GrillSlot slot = Station.GetSlot(SlotIndex);
        return slot != null && slot.GetState() == GrillState.NeedsFlip;
    }

    public void Flip()
    {
        Station.FlipSlot(SlotIndex);
    }
}
