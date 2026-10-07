using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Gives one object a hover outline. Added automatically by
    /// <see cref="HoverOutlineService"/> the first time the player looks at the
    /// object - nothing needs to be wired up on prefabs.
    ///
    /// How it draws: see <see cref="OutlineHullBuilder"/>, which owns the
    /// mask-then-hull technique this and the tutorial highlight share.
    ///
    /// Cost:
    ///   Renderers are built once, on the first hover, and then only enabled and
    ///   disabled. While nothing is hovered every one of them is disabled, so the
    ///   outline system costs exactly zero draw calls, zero render targets and
    ///   zero per-frame work.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverOutline : MonoBehaviour
    {
        private OutlineProxy[] _proxies;
        private bool           _visible;

        // ── Public API ───────────────────────────────────────────────────────────

        public void Show()
        {
            if (_visible) return;

            _proxies ??= OutlineHullBuilder.Build(gameObject,
                                                  HoverOutlineService.MaskMaterial,
                                                  HoverOutlineService.OutlineMaterial);

            _visible = true;
            OutlineHullBuilder.SetVisible(_proxies, true);
        }

        public void Hide()
        {
            if (!_visible) return;

            _visible = false;
            OutlineHullBuilder.SetVisible(_proxies, false);
        }

        /// <summary>
        /// Discards the cached hulls and rebuilds them on the next show. Call this if
        /// the object's meshes are swapped at runtime (a different visual state, an
        /// attachment added) - otherwise the outline keeps tracing the old shape.
        /// </summary>
        public void Rebuild()
        {
            OutlineHullBuilder.Destroy(_proxies);
            _proxies = null;

            if (!_visible) return;

            // Destroy() only takes effect at the end of the frame, but the builder
            // skips anything carrying an OutlineProxy, so the doomed hulls are ignored.
            _proxies = OutlineHullBuilder.Build(gameObject,
                                                HoverOutlineService.MaskMaterial,
                                                HoverOutlineService.OutlineMaterial);
            OutlineHullBuilder.SetVisible(_proxies, true);
        }

        // ── Internals ────────────────────────────────────────────────────────────

        // An object that is switched off (or torn down) while outlined must not come
        // back wearing a stale outline: the service has moved on by then, and its
        // Hide() would be a no-op because this object was never told to stop.
        private void OnDisable() => Hide();
    }
}
