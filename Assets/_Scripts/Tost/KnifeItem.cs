using UnityEngine;

/// <summary>
/// Marker component for the kitchen knife.
/// Extends HoldableItem so it is picked up by InHandController automatically.
/// TostBread and SucukItem check for this type in the player's hand
/// to decide whether knife-specific interactions are available.
/// </summary>
public class KnifeItem : HoldableItem
{
}
