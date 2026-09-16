using UnityEngine;

/// <summary>
/// Keeps the first-person arms from being frustum-culled.
///
/// A SkinnedMeshRenderer normally decides whether it is on screen using the
/// bounding box baked into the mesh at import time, transformed by the root
/// bone. That box is fine for a character standing in the world, but the arms
/// sit right on top of the camera: the box is large, offset from the visible
/// geometry, and never matches the posed arms. As the player looks around it
/// slides out of the viewmodel camera's frustum while the arms themselves are
/// still dead centre on screen, and Unity skips the whole renderer — the arms
/// blink out at certain angles.
///
/// Recomputing the bounds from the actual bone poses every frame removes the
/// mismatch. For two arms this costs nothing measurable.
///
/// Put this on the root of the Arms prefab.
/// </summary>
[DisallowMultipleComponent]
public class ViewmodelBounds : MonoBehaviour
{
    private void Awake()
    {
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;
    }
}
