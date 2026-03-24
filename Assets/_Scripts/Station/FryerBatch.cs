/// <summary>
/// Tracks one frying batch: portion size, elapsed time, and how many
/// slices have been collected or remain in the vat.
/// Ticked each frame by FryerStation.Update().
/// </summary>
public class FryerBatch
{
    public enum Size { Small = 6, Large = 12 }

    public const float CookDuration = 10f;
    public const float ReadyEndTime = 15f;

    public Size   BatchSize   { get; }
    public int    TotalSlices => (int)BatchSize;
    public float  ElapsedTime { get; private set; }

    private int  _collected;
    private bool _cleared;

    public int  CollectedCount => _collected;
    public int  RemainingInVat => _cleared ? 0 : TotalSlices - _collected;

    public FryerBatch(Size size)
    {
        BatchSize = size;
    }

    public void Tick(float deltaTime)
    {
        ElapsedTime += deltaTime;
    }

    public FryState GetState()
    {
        if (ElapsedTime < CookDuration) return FryState.Cooking;
        if (ElapsedTime < ReadyEndTime) return FryState.Ready;
        return FryState.Burnt;
    }

    public void CollectOne() => _collected++;

    /// <summary>
    /// Marks all remaining vat slices as discarded.
    /// The batch stays alive if there are collected slices (for the sauce/finish phase).
    /// </summary>
    public void ClearRemaining() => _cleared = true;
}
