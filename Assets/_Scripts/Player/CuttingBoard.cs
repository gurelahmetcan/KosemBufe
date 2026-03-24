using UnityEngine;

/// <summary>
/// Cutting board station — slices items that have a SliceableItem component.
/// Attach this (alongside a Collider) to the cutting board GameObject.
/// </summary>
public class CuttingBoard : Station
{
    public override bool CanInteract(InHandController hand)
    {
        if (hand.HeldItem == null) return false;
        if (hand.IsSliced) return false;
        return hand.HeldItem.GetComponent<SliceableItem>() != null;
    }

    public override void Interact(InHandController hand)
    {
        hand.SliceHeldItem();
    }
}
