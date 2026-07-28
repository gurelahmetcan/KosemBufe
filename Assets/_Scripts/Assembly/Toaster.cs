using UnityEngine;

namespace Bufe
{
    public class Toaster : MonoBehaviour, IAssemblyInteractable
{
    [Header("References")]
    [Tooltip("The lid component on this toaster's lid child object.")]
    public ToasterLid lid;

    [Tooltip("Optional transform indicating where the bread should rest inside the toaster.")]
    public Transform breadSlot;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private PreparedItem     _bread;
    private ToasterState  _state = ToasterState.Empty;

    private enum ToasterState { Empty, BreadInside, Cooking, Done }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        lid.OnLidClosed += HandleLidClosed;
        lid.OnLidOpened += HandleLidOpened;
    }

    private void OnDestroy()
    {
        lid.OnLidClosed -= HandleLidClosed;
        lid.OnLidOpened -= HandleLidOpened;
    }

    // ── IAssemblyInteractable (toaster body) ──────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // Insert: holding a closed TostBread + toaster is empty + lid is open
        if (!hand.IsHandEmpty && hand.HeldItem is PreparedItem bread)
            return _state == ToasterState.Empty && bread.State == BreadState.Closed && lid.IsOpen;

        // Collect: empty hand + lid open + bread is ready or can be cancelled
        if (hand.IsHandEmpty && lid.IsOpen)
            return _state == ToasterState.Done || _state == ToasterState.BreadInside;

        return false;
    }

    public void Interact(PlayerHand hand)
    {
        if (!hand.IsHandEmpty && hand.HeldItem is PreparedItem)
        {
            InsertBread(hand);
            return;
        }

        if (hand.IsHandEmpty && lid.IsOpen &&
            (_state == ToasterState.Done || _state == ToasterState.BreadInside))
        {
            CollectBread(hand);
        }
    }

    // ── Called by TostBread when the player picks it up ───────────────────────

    /// <summary>
    /// Notifies the toaster that the bread has been taken out, resetting state.
    /// Called via TostBread.OnPickedUp().
    /// </summary>
    public void OnPreparedItemPickedUp()
    {
        _bread = null;
        _state = ToasterState.Empty;
        Debug.Log("[Toaster] Bread collected — toaster reset.");
    }

    // ── State transitions ─────────────────────────────────────────────────────

    private void InsertBread(PlayerHand hand)
    {
        _bread = hand.Release() as PreparedItem;

        Transform anchor = breadSlot != null ? breadSlot : transform;
        _bread.transform.SetParent(anchor);
        _bread.transform.localPosition = Vector3.zero;
        _bread.transform.localRotation = Quaternion.identity;

        // Colliders intentionally left DISABLED — player collects via the
        // toaster body interaction (CollectBread), not by clicking the bread.

        _bread.PlaceInToaster(this);
        _state = ToasterState.BreadInside;

        Debug.Log("[Toaster] Bread inserted. Close the lid to start toasting.");
    }

    private void CollectBread(PlayerHand hand)
    {
        // PlayerHand.PickUp finds disabled colliders via GetComponentsInChildren,
        // so no pre-enabling needed.
        hand.PickUp(_bread);
    }

    private void HandleLidClosed()
    {
        // Only the first close starts toasting; re-closing during cooking is a no-op.
        if (_state != ToasterState.BreadInside) return;

        _bread.StartToasting();
        _state = ToasterState.Cooking;

        // Lock the lid shut while it cooks — no peeking until it's done.
        lid.SetLocked(true);

        // The toast SFX plays twice; when it ends, the toast is done (OnToastingComplete).
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfxSequence(Sfx.Toast, 2, OnToastingComplete);
        else
            OnToastingComplete();   // no audio in scene → finish immediately

        Debug.Log("[Toaster] Toasting started — sound plays twice, then it's ready.");
    }

    /// <summary>Called when the toast SFX sequence finishes — the toast is cooked.</summary>
    private void OnToastingComplete()
    {
        // Always release the lid when the sequence ends, so it can never stay stuck shut.
        lid.SetLocked(false);

        // Guard against the toaster having been emptied/reset while the sound played.
        if (_state != ToasterState.Cooking || _bread == null) return;

        _state = ToasterState.Done;
        _bread.FinishToasting();
        Debug.Log("[Toaster] Toast is ready!");
    }

    private void HandleLidOpened()
    {
        if (_state != ToasterState.Done) return;
        Debug.Log("[Toaster] Lid open. Pick up your toast!");
    }
}
}
