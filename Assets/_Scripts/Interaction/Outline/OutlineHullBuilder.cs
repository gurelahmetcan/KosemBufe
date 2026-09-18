using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bufe
{
    /// <summary>
    /// Builds the inverted-hull renderers that draw an object's outline.
    ///
    /// Two systems draw outlines — <see cref="HoverOutline"/> (what a click would
    /// act on) and <see cref="HighlightOutline"/> (the pulsing tutorial target) —
    /// and the tricky part is the same for both: one mask renderer per mesh to
    /// stamp the silhouette into the stencil buffer, one hull renderer to draw the
    /// ring of colour outside it. Only the materials differ, so the construction
    /// lives here and each system passes its own pair.
    ///
    /// Not handled on purpose:
    ///   SkinnedMeshRenderer. Customers are not outlined, and skinning the hull
    ///   would mean a second skinning pass per outlined character.
    /// </summary>
    internal static class OutlineHullBuilder
    {
        private const string MaskName = "__OutlineMask";
        private const string HullName = "__OutlineHull";

        // Reused across every Build() call so scanning an object allocates nothing.
        private static readonly List<MeshRenderer> s_scan = new List<MeshRenderer>(16);

        /// <summary>
        /// Creates a mask + hull pair under every MeshRenderer beneath
        /// <paramref name="root"/>, all of them starting hidden. Returns an empty
        /// array when there is nothing to trace or the materials are missing, so
        /// callers never have to null-check the result.
        /// </summary>
        internal static OutlineProxy[] Build(GameObject root, Material maskMaterial, Material outlineMaterial)
        {
            if (root == null || maskMaterial == null || outlineMaterial == null)
                return Array.Empty<OutlineProxy>();

            // Inactive renderers are included: a hull is a child of the renderer it
            // traces, so it inherits that renderer's active state for free and starts
            // drawing by itself once the source is switched back on.
            s_scan.Clear();
            root.GetComponentsInChildren(true, s_scan);

            List<OutlineProxy> built = null;

            for (int i = 0; i < s_scan.Count; i++)
            {
                MeshRenderer source = s_scan[i];

                // Never outline an outline — this also covers nested objects that
                // were outlined on their own earlier (items on the display rack),
                // and the other system's hulls when both are on the same object.
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

            return built != null ? built.ToArray() : Array.Empty<OutlineProxy>();
        }

        /// <summary>Destroys a set of proxies built earlier. Safe with nulls and with a null array.</summary>
        internal static void Destroy(OutlineProxy[] proxies)
        {
            if (proxies == null) return;

            for (int i = 0; i < proxies.Length; i++)
                if (proxies[i] != null)
                    UnityEngine.Object.Destroy(proxies[i].gameObject);
        }

        internal static void SetVisible(OutlineProxy[] proxies, bool visible)
        {
            if (proxies == null) return;

            for (int i = 0; i < proxies.Length; i++)
            {
                OutlineProxy proxy = proxies[i];
                if (proxy != null)
                    proxy.SetVisible(visible);
            }
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
