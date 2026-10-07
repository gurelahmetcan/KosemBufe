using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Randomises a customer's look by swapping one material slot on the body
    /// renderer to a random texture variant. Put this on a customer body prefab
    /// and assign the renderer + the material variants; it picks one on spawn.
    ///
    /// Appearance is independent of CustomerArchetype (behaviour): the same body
    /// prefab can represent many characters just by changing this material.
    /// Leave <see cref="variants"/> empty to keep the prefab's authored look.
    /// </summary>
    public class CustomerAppearance : MonoBehaviour
    {
        [Tooltip("Renderer to swap the material on.")]
        [SerializeField] private Renderer bodyRenderer;

        [Tooltip("Material slot to swap.")]
        [SerializeField] private int materialIndex = 0;

        [Tooltip("Random variant on spawn. Empty = prefab material.")]
        [SerializeField] private Material[] variants;

        private void Awake() => Randomize();

        /// <summary>Picks a random variant and applies it to the configured material slot.</summary>
        public void Randomize()
        {
            if (variants == null || variants.Length == 0) return;
            Apply(variants[Random.Range(0, variants.Length)]);
        }

        /// <summary>
        /// Forces a specific material onto the configured slot, overriding the random pick.
        /// Used for scripted characters with a fixed look (e.g. the tutorial dayı).
        /// </summary>
        public void Apply(Material material)
        {
            if (bodyRenderer == null || material == null) return;

            // Assign the material asset directly (sharedMaterials) so we don't leak
            // per-instance material copies; identical variants stay batchable.
            Material[] mats = bodyRenderer.sharedMaterials;
            if (materialIndex < 0 || materialIndex >= mats.Length) return;

            mats[materialIndex] = material;
            bodyRenderer.sharedMaterials = mats;
        }
    }
}
