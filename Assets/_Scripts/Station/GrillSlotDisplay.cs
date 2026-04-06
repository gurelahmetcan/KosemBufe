using UnityEngine;
using DG.Tweening;

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

    private bool _isFlipping;

    // ── IInteractable — click to collect ─────────────────────────────────────

    public bool CanInteract(InHandController hand)
    {
        // Empty hand → collect the item from the grill (not during flip anim)
        return hand.HeldItem == null && !_isFlipping;
    }

    public void Interact(InHandController hand)
    {
        Station.CollectFromSlot(SlotIndex, hand);
    }

    // ── IFlippable — press F to flip ─────────────────────────────────────────

    public bool CanFlip()
    {
        if (_isFlipping) return false;
        GrillSlot slot = Station.GetSlot(SlotIndex);
        return slot != null && slot.GetState() == GrillState.NeedsFlip;
    }

    public void Flip()
    {
        Station.FlipSlot(SlotIndex);
        PlayFlipAnimation();
    }

    // ── Flip animation ───────────────────────────────────────────────────────

    private void PlayFlipAnimation()
    {
        _isFlipping = true;

        // Quick jump + 180° rotation on the local X axis
        Vector3 startPos = transform.localPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(startPos.y + 0.15f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(transform.DOLocalRotate(new Vector3(180f, 0f, 0f), 0.3f, RotateMode.LocalAxisAdd)
                          .SetEase(Ease.InOutQuad));
        seq.Append(transform.DOLocalMoveY(startPos.y, 0.15f).SetEase(Ease.InQuad));
        seq.OnComplete(() => _isFlipping = false);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
