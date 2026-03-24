/// <summary>
/// Internal state of a frying batch.
/// Determined by elapsed time since the batch started.
/// </summary>
public enum FryState
{
    Cooking,
    Ready,
    Burnt
}
