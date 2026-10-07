namespace Bufe
{
    /// <summary>
    /// Tracks the current state of a TostBread through the preparation pipeline.
    /// Whole → Cut (knife) → Open (placed on station) → Closed → Toasting → Toasted
    /// </summary>
    public enum BreadState
    {
        Full,
        Half,
        Open,
        Closed,
        Toasting,
        Toasted
    }
}
