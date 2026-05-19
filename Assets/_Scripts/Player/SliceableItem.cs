using Bufe;
using UnityEngine;

/// <summary>
/// Add-on component for items that can be sliced on the cutting board.
/// Requires an InteractableItem on the same GameObject.
/// </summary>
[RequireComponent(typeof(InteractableItem))]
public class SliceableItem : MonoBehaviour
{
    [Tooltip("Prefab shown in the player's hand after the item has been sliced.")]
    public GameObject slicedHandPrefab;
}
