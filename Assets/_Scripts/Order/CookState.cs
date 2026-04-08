/// <summary>
/// Tracks the cooking state of an ingredient or finalized stack.
/// Set by cooking appliances (Grill, Toaster) during processing.
/// </summary>
public enum CookState
{
    Raw,
    Cooked,
    Toasted,
    Burnt
}
