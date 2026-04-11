/// <summary>
/// Cooking phase of a single ingredient sitting on a Grill slot.
/// Derived from elapsed time on the current side and whether the item
/// has been flipped.
///
///   CookingSide1 → first side is cooking, player waits.
///   NeedsFlip    → first side is done, player must press F to flip before it burns.
///   CookingSide2 → second side is cooking after a flip.
///   Cooked       → fully cooked on both sides, player can click to collect.
///   Burnt        → left too long, ingredient is ruined.
/// </summary>
public enum GrillSlotState
{
    CookingSide1,
    NeedsFlip,
    CookingSide2,
    Cooked,
    Burnt
}
