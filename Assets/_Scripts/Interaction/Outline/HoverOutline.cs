using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bufe
{
    /// <summary>
    /// Gives one object a hover outline. Added automatically by
    /// <see cref="HoverOutlineService"/> the first time the player looks at the
    /// object — nothing needs to be wired up on prefabs.
    ///
    /// How it draws:
    ///   Every MeshRenderer under this object gets two child renderers that reuse
    ///   the SAME Mesh asset — no mesh is duplicated and no extra memory is
    ///   allocated. The first stamps the object's silhouette into the stencil
    ///   buffer and writes no colour; the second draws the mesh with front faces
    ///   culled and back faces pushed a couple of pixels outwards, and is rejected
    ///   wherever the first one stamped. The two shaders explain why: without the
    ///   stencil step, an object built from several overlapping shells draws a
    ///   contour around each shell instead of one contour around the whole thing.
    ///
    /// Cost:
    ///   Renderers are built once, on the first hover, and then only enabled and
    ///   disabled. While nothing is hovered every one of them is disabled, so the
    ///   outline system costs exactly zero draw calls, zero render targets and
    ///   zero per-frame work.
    ///
    /// Not handled on purpose:
    ///   SkinnedMeshRenderer. Customers are not outlined, and skinning the hull
    ///   would mean a second skinning pass per hovered character.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverOutline : MonoBehaviour
    {
        private const string MaskName = "__OutlineMask";
        private const string HullName = "__OutlineHull";

        // Reused across every Build() call so scanning an object allocates nothing.
        private static readonly List<MeshRenderer> s_scan = new List<MeshRenderer>(16);

        private OutlineProxy[] _proxies;
        private bool           _visible;

        // ── Public API ───────────────────────────────────────────────────────────

        public void Show()
        {
            if (_visible) return;

            if (_proxies == null) Build();

            _visible = true;
            SetProxiesVisible(true);
        }

        public void Hide()
        {
            if (!_visible) return;

            _visible = false;
            SetProxiesVisible(false);
        }

        /// <summary>
        /// Discards the cached hulls and rebuilds them on the next show. Call this if
        /// the object's meshes are swapped at runtime (a different visual state, an
        /// attachment added) — otherwise the outline keeps tracing the old shape.
        /// </summary>
        public void Rebuild()
        {
            if (_proxies != null)
            {
                for (int i = 0; i < _proxies.Length; i++)
                    if (_proxies[i] != null)
                        Destroy(_proxies[i].gameObject);
            }

            _proxies = null;

            if (!_visible) return;

            // Destroy() only takes effect at the end of the frame, but Build() skips
            // anything carrying an OutlineProxy, so the doomed hulls are ignored.
            Build();
            SetProxiesVisible(true);
        }

        // ── Internals ────────────────────────────────────────────────────────────

        // An object that is switched off (or torn down) while outlined must not come
        // back wearing a stale outline: the service has moved on by then, and its
        // Hide() would be a no-op because this object was never told to stop.
        private void OnDisable() => Hide();

        private void SetProxiesVisible(bool visible)
        {
            for (int i = 0; i < _proxies.Length; i++)
            {
                OutlineProxy proxy = _proxies[i];
                if (proxy != null)
                    proxy.SetVisible(visible);
            }
        }

        private void Build()
        {
            Material maskMaterial    = HoverOutlineService.MaskMaterial;
            Material outlineMaterial = HoverOutlineService.OutlineMaterial;

            if (maskMaterial == null || outlineMaterial == null)
            {
                _proxies = Array.Empty<OutlineProxy>();
                return;
            }

            // Inactive renderers are included: a hull is a child of the renderer it
            // traces, so it inherits that renderer's active state for free and starts
            // drawing by itself once the source is switched back on.
            s_scan.Clear();
            GetComponentsInChildren(true, s_scan);

            List<OutlineProxy> built = null;

            for (int i = 0; i < s_scan.Count; i++)
            {
                MeshRenderer source = s_scan[i];

                // Never outline an outline — this also covers nested objects that
                // were hovered on their own earlier (items on the display rack).
                if (source.GetComponent<OutlineProxy>() != null) continue;

                MeshFilter filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                built ??= new List<OutlineProxy>(s_scan.Count * 2);

                // Order between the two is decided by the shaders' render queues
                // (mask at Geometry+50, outline at Geometry+100), which URP sorts
                // on before anything else — not by the order they are created here.
                built.Add(CreateProxy(source, filter.sharedMesh, maskMaterial,    MaskName));
                built.Add(CreateProxy(source, filter.sharedMesh, outlineMaterial, HullName));
            }

            _proxies = built != null ? built.ToArray() : Array.Empty<OutlineProxy>();
        }

        private static OutlineProxy CreateProxy(MeshRenderer source, Mesh mesh, Material material, string name)
        {
            var go = new GameObject(name);
            go.layer = source.gameObject.layer;

            Transform t = go.transform;
            t.SetParent(source.transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale    = Vector3.one;

            // No collider is added, so the crosshair ray never sees the hull.
            var proxy = go.AddComponent<OutlineProxy>();

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();

            // One slot per submesh — with fewer slots Unity silently drops the
            // remaining submeshes and the outline would only trace part of the shape.
            int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
            var materials = new Material[subMeshCount];
            for (int i = 0; i < subMeshCount; i++) materials[i] = material;
            renderer.sharedMaterials = materials;

            // The hull is pure silhouette colour: it must not take part in shadows,
            // lighting, probes or motion vectors.
            renderer.shadowCastingMode          = ShadowCastingMode.Off;
            renderer.receiveShadows             = false;
            renderer.lightProbeUsage            = LightProbeUsage.Off;
            renderer.reflectionProbeUsage       = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic  = false;

            proxy.Bind(renderer);
            return proxy;
        }
    }
}
