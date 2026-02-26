using UnityEngine;

/// <summary>
/// ScriptableObject that defines everything about one sliceable item type
/// (sucuk, cheese, bread, etc.).
///
/// To create a new item: right-click in the Project window →
///   Kosem Bufe → Sliceable Item Data
/// Fill in its fields, then assign it to a SliceableSource in the scene.
/// </summary>
[CreateAssetMenu(fileName = "NewSliceableItem", menuName = "Kosem Bufe/Sliceable Item Data")]
public class SliceableItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in the hand status UI (e.g. 'Sucuk', 'Kasar').")]
    public string itemName = "Item";

    [Header("Prefabs")]
    [Tooltip("Prefab shown in the player's hand for the full (uncut) item.")]
    public GameObject boardPrefab;

    [Tooltip("Prefab shown in the player's hand after the item has been sliced.")]
    public GameObject slicePrefab;
}
