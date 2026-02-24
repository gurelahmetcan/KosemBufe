using UnityEngine;

/// <summary>
/// Attach to any GameObject in the scene that the player can click to place
/// on the cutting board (hanging sucuk, counter cheese, fridge item, etc.).
///
/// Setup:
///  1. Add this component to the source GameObject.
///  2. Assign the matching SliceableItemData asset to the ItemData field.
///  3. Make sure the GameObject (or any child) has a Collider so raycasts hit it.
/// </summary>
public class SliceableSource : MonoBehaviour
{
    [Tooltip("The data asset that describes this item type (prefabs, speeds, max slices, etc.).")]
    public SliceableItemData itemData;
}
