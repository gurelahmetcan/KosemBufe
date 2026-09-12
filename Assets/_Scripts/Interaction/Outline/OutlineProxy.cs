using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Marker sitting on the generated hull renderer that draws an object's outline.
    ///
    /// It exists for two reasons:
    ///   1. <see cref="HoverOutline"/> skips any renderer carrying this component, so
    ///      scanning an object that already has proxies (or that contains a nested
    ///      outlined object, e.g. items sitting on the display rack) never produces
    ///      an outline of an outline.
    ///   2. It keeps the renderer reference handy, so showing and hiding is a single
    ///      bool flip instead of a hierarchy activation walk.
    ///
    /// Never added by hand — <see cref="HoverOutline"/> creates these at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutlineProxy : MonoBehaviour
    {
        private MeshRenderer _renderer;

        internal void Bind(MeshRenderer meshRenderer)
        {
            _renderer = meshRenderer;
            _renderer.enabled = false;
        }

        internal void SetVisible(bool visible)
        {
            if (_renderer != null)
                _renderer.enabled = visible;
        }
    }
}
