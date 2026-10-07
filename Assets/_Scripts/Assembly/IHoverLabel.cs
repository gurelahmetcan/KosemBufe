namespace Bufe
{
    /// <summary>
    /// Implemented by anything that wants to show a custom hover label when the player aims at it
    /// (e.g. "Cheese Block - 4 left"). PlayerHand queries this first, before its generic fallbacks.
    /// </summary>
    public interface IHoverLabel
    {
        string HoverLabel { get; }
    }
}
