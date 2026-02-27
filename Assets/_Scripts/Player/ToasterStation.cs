using UnityEngine;

/// <summary>
/// Attach this (and a Collider) to the Toaster object in the scene.
///
/// Trigger: Click the toaster while holding a loaded HolderItem (StoredCount > 0)
///          → starts the ToasterMinigame.
///
/// successItem – HoldableItem the player receives when the progress gauge fills.
/// failureItem – HoldableItem the player receives when the progress gauge empties.
///
/// Both items should be HoldableItem GameObjects placed somewhere in the scene
/// (they don't need a Collider since they are never directly clicked).
/// </summary>
public class ToasterStation : MonoBehaviour
{
    [Tooltip("HoldableItem given when the minigame is completed successfully (Full Progress).")]
    public HoldableItem successItem;

    [Tooltip("HoldableItem given when the progress gauge empties (Burned).")]
    public HoldableItem failureItem;
}
