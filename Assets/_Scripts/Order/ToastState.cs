/// <summary>
/// Tracks the toasting state of a finalized stack.
/// Set by the Toaster appliance: None → Toasted → Burnt.
/// Lives on the FinalizedStack, independent of individual ingredient CookStates.
/// </summary>
public enum ToastState
{
    None,
    Toasted,
    Burnt
}
