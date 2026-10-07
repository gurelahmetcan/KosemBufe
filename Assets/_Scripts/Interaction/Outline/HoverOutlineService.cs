using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Owns the single "currently outlined object" slot.
    ///
    /// Only one object is ever outlined at a time, which is what makes the whole
    /// system cheap: there is no manager updating a list, no renderer feature and no
    /// full-screen pass. Callers just push a target whenever it changes; switching
    /// costs one Hide and one Show.
    ///
    /// <see cref="PlayerHand"/> is the only caller today - it pushes whatever a
    /// left-click would act on right now, so the outline can never advertise an
    /// interaction the click will not perform.
    /// </summary>
    public static class HoverOutlineService
    {
        // Material assets under a Resources folder - see Assets/Resources/.
        private const string OutlineResourcePath = "HoverOutline";
        private const string MaskResourcePath    = "HoverOutlineMask";

        private const string OutlineShaderName = "Bufe/HoverOutline";
        private const string MaskShaderName    = "Bufe/HoverOutlineMask";

        private static HoverOutline _current;

        private static Material _outlineMaterial;
        private static Material _maskMaterial;
        private static bool     _materialsResolved;

        /// <summary>
        /// The visible outline material. Shared by every hull in the game, with no
        /// per-object property blocks, so the hulls stay SRP Batcher compatible.
        /// Tune colour and width on the asset itself.
        /// </summary>
        public static Material OutlineMaterial
        {
            get
            {
                ResolveMaterials();
                return _outlineMaterial;
            }
        }

        /// <summary>
        /// Stencil-only companion that stamps the object's silhouette before the
        /// outline draws, so the outline cannot appear inside the object. See
        /// HoverOutlineMask.shader for why an inverted hull needs this.
        /// </summary>
        public static Material MaskMaterial
        {
            get
            {
                ResolveMaterials();
                return _maskMaterial;
            }
        }

        private static void ResolveMaterials()
        {
            if (_materialsResolved) return;
            _materialsResolved = true;

            _outlineMaterial = Load(OutlineResourcePath, OutlineShaderName);
            _maskMaterial    = Load(MaskResourcePath,    MaskShaderName);
        }

        private static Material Load(string resourcePath, string shaderName)
        {
            Material material = Resources.Load<Material>(resourcePath);
            if (material != null) return material;

            // Fallback for the case where the asset was moved or deleted: build one
            // from the shader so hover feedback still works.
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[HoverOutlineService] Neither Resources/{resourcePath}.mat nor the " +
                               $"shader '{shaderName}' could be found. Hover outlines are off.");
                return null;
            }

            Debug.LogWarning($"[HoverOutlineService] Resources/{resourcePath}.mat is missing - " +
                             $"created a temporary material from '{shaderName}'.");

            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        /// <summary>
        /// Outlines <paramref name="root"/> and everything under it, clearing whatever
        /// was outlined before. Pass null to clear. The component that does the work is
        /// added on first use, so no prefab authoring is required.
        /// </summary>
        public static void SetTarget(GameObject root)
        {
            HoverOutline next = null;

            if (root != null)
            {
                next = root.GetComponent<HoverOutline>();
                if (next == null) next = root.AddComponent<HoverOutline>();
            }

            if (_current == next) return;

            // Unity's == treats a destroyed object as null, so an object that was
            // torn down while outlined is skipped here rather than throwing.
            if (_current != null) _current.Hide();

            _current = next;

            if (_current != null) _current.Show();
        }

        public static void Clear() => SetTarget(null);

        /// <summary>
        /// Statics survive scene loads (and play-mode entry when domain reload is
        /// disabled), so they are reset explicitly before each run.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _current           = null;
            _outlineMaterial   = null;
            _maskMaterial      = null;
            _materialsResolved = false;
        }
    }
}
