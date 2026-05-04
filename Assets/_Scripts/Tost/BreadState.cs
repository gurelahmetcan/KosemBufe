/// <summary>
/// Tracks the current state of a TostBread through the preparation pipeline.
/// Whole → Cut (knife) → Open (placed on station) → Closed → Toasting → Toasted
/// </summary>
public enum BreadState
{
    Whole,
    Cut,
    Open,
    Closed,
    Toasting,
    Toasted
}
