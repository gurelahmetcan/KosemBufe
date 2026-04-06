/// <summary>
/// Tracks the cooking phase of a single item on the grill.
/// Derived from elapsed time on the current side and whether the item
/// has been flipped.
/// </summary>
public enum GrillState
{
    /// <summary>First side is cooking. Player should wait.</summary>
    CookingSide1,

    /// <summary>First side is done. Player must press F to flip before it burns.</summary>
    NeedsFlip,

    /// <summary>Second side is cooking after a flip. Player should wait.</summary>
    CookingSide2,

    /// <summary>Fully cooked on both sides. Player can click to collect.</summary>
    Cooked,

    /// <summary>Left too long — item is ruined.</summary>
    Burnt
}
