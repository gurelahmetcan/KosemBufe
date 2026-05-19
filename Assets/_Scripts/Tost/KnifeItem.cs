/// <summary>
/// Marker component for the kitchen knife.
/// Extends InteractableItem so PlayerHand can pick it up.
/// TostBread and SucukItem check for this type in the player's hand
/// to decide whether knife-specific interactions are available.
/// </summary>
namespace Bufe
{
    public class KnifeItem : InteractableItem
    {
    }
}