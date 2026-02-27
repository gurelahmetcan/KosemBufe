using UnityEngine;

/// <summary>
/// Base component for any item the player can pick up and hold.
/// Attach this (and a Collider) to any source object in the scene.
///
/// For items that can also be sliced on the cutting board,
/// add a SliceableItem component alongside this one.
/// </summary>
public class HoldableItem : MonoBehaviour
{
    [Tooltip("Display name shown in the hand status UI (e.g. 'Sucuk', 'Ekmek').")]
    public string itemName = "Item";

    [Tooltip("Prefab instantiated at the hand display point when the player picks this up.")]
    public GameObject handPrefab;
}
