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
    [Tooltip("Display name for this item (for debugging / UI).")]
    public string itemName = "Item";

    [Header("Prefabs")]
    [Tooltip("Prefab spawned on the cutting board when the player grabs this item.")]
    public GameObject boardPrefab;

    [Tooltip("Prefab instantiated for each slice produced.")]
    public GameObject slicePrefab;

    [Header("Slice Settings")]
    [Tooltip("How many slices can be cut before the board item disappears.")]
    public int maxSlices = 5;

    [Tooltip("Speed (units/sec) the knife descends.")]
    public float sliceSpeed = 3f;

    [Tooltip("Speed (units/sec) the knife resets after each cut.")]
    public float resetSpeed = 8f;

    [Header("Slice Animation")]
    [Tooltip("Speed (units/sec) each slice flies to the stack.")]
    public float sliceFlySpeed = 6f;

    [Tooltip("Y gap between stacked slices on the plate.")]
    public float stackSpacing = 0.1f;
}
