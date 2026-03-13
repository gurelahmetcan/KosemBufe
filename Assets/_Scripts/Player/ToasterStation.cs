using UnityEngine;

/// <summary>
/// Attach this (and a Collider) to the Toaster object in the scene.
///
/// Trigger: Click the toaster while holding a loaded HolderItem (StoredCount > 0)
///          → starts the ToasterMinigame.
///
/// After the minigame the same holder is returned to the player's hand
/// with its cookState set to Cooked (success) or Burnt (failure).
/// </summary>
public class ToasterStation : MonoBehaviour
{
}
