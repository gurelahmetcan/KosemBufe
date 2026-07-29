using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bufe
{
    /// <summary>
    /// Keeps the URP Depth of Field (Bokeh) focused on whatever the camera is looking at,
    /// so close-up interactables (the cash register keypad, the counter) stay sharp while the
    /// background keeps its blur. Raycasts from screen center each frame and eases the focus
    /// distance toward the hit distance.
    ///
    /// Needs the DoF override on the assigned Volume set to Bokeh mode.
    /// </summary>
    public class CameraAutoFocus : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Volume holding the Depth Of Field override (e.g. Global Volume).")]
        [SerializeField] private Volume volume;

        [Tooltip("Camera to focus from. Empty = Camera.main.")]
        [SerializeField] private Camera focusCamera;

        [Header("Focus")]
        [Tooltip("Farthest focus distance when nothing is hit (e.g. looking at the sky).")]
        [SerializeField] private float maxFocusDistance = 25f;

        [Tooltip("How quickly the focus eases to the new target. Higher = snappier.")]
        [SerializeField] private float focusSpeed = 10f;

        [Tooltip("Which layers the focus ray considers.")]
        [SerializeField] private LayerMask raycastMask = ~0;

        private DepthOfField _dof;

        private void Start()
        {
            if (focusCamera == null) focusCamera = Camera.main;
            if (volume != null && volume.profile != null)
                volume.profile.TryGet(out _dof);   // volume.profile is a runtime copy — safe to tweak
        }

        private void LateUpdate()
        {
            if (_dof == null || focusCamera == null) return;

            Ray ray = focusCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float target = Physics.Raycast(ray, out RaycastHit hit, maxFocusDistance, raycastMask)
                ? hit.distance
                : maxFocusDistance;

            _dof.focusDistance.value = Mathf.Lerp(_dof.focusDistance.value, target,
                                                  Time.deltaTime * focusSpeed);
        }
    }
}
