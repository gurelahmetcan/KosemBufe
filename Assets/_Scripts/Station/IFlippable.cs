/// <summary>
/// Contract for objects that can be flipped by the player pressing F.
/// Implemented by GrillSlotDisplay.
/// </summary>
public interface IFlippable
{
    bool CanFlip();
    void Flip();
}
