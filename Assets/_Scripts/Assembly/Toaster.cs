using UnityEngine;

/// <summary>
/// Toaster appliance — accepts a FinalizedStack (closed sandwich), hides it
/// inside the toaster, and applies a ToastState after processing.
///
/// The stack is hidden while inside (SetActive false) and shown again on collect.
/// ToastState is set on the FinalizedStack: None → Toasted, or → Burnt if left too long.
///
/// Only accepts items with the Toastable process flag, which PrepStation
/// automatically sets on every FinalizedStack.
///
/// Setup:
///   1. Place a GameObject with a Collider (the toaster body).
///   2. Add this component.
///   3. Tune processTime and burnGraceTime.
/// </summary>
public class Toaster : Appliance
{
    // ── Runtime ──────────────────────────────────────────────────────────────

    private FinalizedStack _stack;

    // ── Appliance overrides ──────────────────────────────────────────────────

    protected override bool CanAccept(InteractableItem item)
    {
        return item is FinalizedStack && item.supportedProcesses.HasFlag(ProcessType.Toastable);
    }

    protected override void OnItemInserted()
    {
        _stack = _storedItem as FinalizedStack;

        // Hide the stack — it's "inside" the toaster
        _storedItem.gameObject.SetActive(false);

        Debug.Log($"[Toaster] Inserted '{_stack.itemName}' — toasting for {processTime}s");
    }

    protected override void OnProcessingComplete()
    {
        _stack.toastState = ToastState.Toasted;
        Debug.Log($"[Toaster] '{_stack.itemName}' is TOASTED! Collect before it burns ({burnGraceTime}s).");
    }

    protected override void OnBurned()
    {
        _stack.toastState = ToastState.Burnt;
        Debug.Log($"[Toaster] '{_stack.itemName}' is BURNT!");
    }

    protected override void OnBeforeCollect()
    {
        // Show the stack again before giving it back to the player
        if (_storedItem != null)
            _storedItem.gameObject.SetActive(true);

        _stack = null;
    }
}
