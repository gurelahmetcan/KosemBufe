using UnityEngine;

/// <summary>
/// Add-on component for items that can be sliced on the cutting board.
/// Requires a HoldableItem on the same GameObject — that component provides
/// the item name and the full (uncut) hand prefab.
///
/// When the player clicks the cutting board while holding this item,
/// the hand visual swaps to slicedHandPrefab and the UI updates to "Sliced …".
/// </summary>
[RequireComponent(typeof(HoldableItem))]
public class SliceableItem : MonoBehaviour
{
    [Tooltip("Prefab shown in the player's hand after the item has been sliced.")]
    public GameObject slicedHandPrefab;
}
