/// <summary>
/// Optional secondary interaction, triggered by an empty-hand right-click.
/// Used for "take back" style actions (e.g. lifting a bread off the prep
/// station) that shouldn't collide with the primary left-click interaction.
/// </summary>
namespace Bufe
{
    public interface ISecondaryInteractable
    {
        /// <summary>Can the player secondary-interact right now given the current hand state?</summary>
        bool CanSecondaryInteract(PlayerHand hand);

        /// <summary>Execute the secondary interaction.</summary>
        void SecondaryInteract(PlayerHand hand);
    }
}
