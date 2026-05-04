using UnityEngine;

/// <summary>
/// Toaster for the TostBread pipeline. Replaces the previous Appliance-based version.
///
/// Flow:
///   1. Player holds a closed TostBread and clicks the toaster body → bread placed in slot.
///   2. Player click-drags the ToasterLid down until it closes → cooking timer starts.
///   3. Timer only counts while the lid is closed. Opening the lid pauses it;
///      closing again resumes from where it left off.
///   4. After cookTime total closed-lid seconds the toast is ready.
///
/// Setup:
///   1. Create a Toaster root GameObject with a Collider. Attach this component.
///   2. Create a child GameObject for the lid with a Collider. Attach ToasterLid.
///      Assign it to the 'lid' field.
///   3. Optionally create a child Transform at the bread resting point and assign
///      it to 'breadSlot'. If left empty the bread centres on the toaster root.
/// </summary>
public class Toaster : MonoBehaviour, IAssemblyInteractable
{
    [Header("Timing")]
    [Tooltip("Seconds from lid-close to toast ready.")]
    public float cookTime = 10f;

    [Header("References")]
    [Tooltip("The lid component on this toaster's lid child object.")]
    public ToasterLid lid;

    [Tooltip("Optional transform indicating where the bread should rest inside the toaster.")]
    public Transform breadSlot;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private TostBread     _bread;
    private float         _timer;
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

    private void Update()
    {
        if (_state != ToasterState.Cooking) return;
        if (lid.IsOpen) return; // lid open → timer paused

        _timer += Time.deltaTime;
        if (_timer >= cookTime)
        {
            _state = ToasterState.Done;
            _bread.FinishToasting();
            Debug.Log("[Toaster] Toast is ready!");
        }
    }

    // ── IAssemblyInteractable (toaster body) ──────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // Insert: holding a closed TostBread + toaster is empty + lid is open
        if (!hand.IsHandEmpty && hand.HeldItem is TostBread bread)
            return _state == ToasterState.Empty && bread.State == BreadState.Closed && lid.IsOpen;

        return false;
    }

    public void Interact(PlayerHand hand)
    {
        if (!hand.IsHandEmpty && hand.HeldItem is TostBread)
            InsertBread(hand);
    }

    // ── Called by TostBread when the player picks it up ───────────────────────

    /// <summary>
    /// Notifies the toaster that the bread has been taken out, resetting state.
    /// Called via TostBread.OnPickedUp().
    /// </summary>
    public void OnTostBreadPickedUp()
    {
        _bread = null;
        _state = ToasterState.Empty;
        Debug.Log("[Toaster] Bread collected — toaster reset.");
    }

    // ── State transitions ─────────────────────────────────────────────────────

    private void InsertBread(PlayerHand hand)
    {
        _bread = hand.Release() as TostBread;

        // Position the bread at the slot (visible so the player can still cancel)
        Transform anchor = breadSlot != null ? breadSlot : transform;
        _bread.transform.SetParent(anchor);
        _bread.transform.localPosition = Vector3.zero;
        _bread.transform.localRotation = Quaternion.identity;

        // Re-enable colliders so the player can pick the bread back up before cooking
        foreach (Collider col in _bread.GetComponentsInChildren<Collider>())
            col.enabled = true;

        _bread.PlaceInToaster(this);
        _state = ToasterState.BreadInside;

        Debug.Log("[Toaster] Bread inserted. Close the lid to start toasting.");
    }

    private void HandleLidClosed()
    {
        if (_state == ToasterState.BreadInside)
        {
            // First close — start cooking
            _bread.StartToasting();
            _state = ToasterState.Cooking;
            _timer = 0f;
            Debug.Log($"[Toaster] Cooking started — {cookTime}s of closed-lid time needed.");
        }
        // If state is already Cooking, the lid was reopened and closed again.
        // Update() will simply resume ticking _timer — nothing extra needed.
    }

    private void HandleLidOpened()
    {
        if (_state != ToasterState.Done) return;
        Debug.Log("[Toaster] Lid open. Pick up your toast!");
    }
}
