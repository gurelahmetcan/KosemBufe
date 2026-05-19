/// <summary>
/// Contract for any object the player can interact with via PlayerHand.
/// Implemented by stations, appliances, holders, and scene objects (door, shutter).
/// </summary>
namespace Bufe
{
    public interface IAssemblyInteractable
    {
        /// <summary>Can the player interact right now given the current hand state?</summary>
        bool CanInteract(PlayerHand hand);

        /// <summary>Execute the interaction.</summary>
        void Interact(PlayerHand hand);
    }
}