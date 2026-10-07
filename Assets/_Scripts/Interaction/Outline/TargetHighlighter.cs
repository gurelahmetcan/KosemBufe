using UnityEngine;
using UnityEngine.Rendering;

namespace Bufe
{
    /// <summary>
    /// Draws a pulsing outline on one object at a time to say "go here next" -
    /// the tutorial's pointing finger.
    ///
    /// It reuses the hover outline's technique (<see cref="OutlineHullBuilder"/>)
    /// but never its materials. The hover outline's pair is shared by every hull in
    /// the game with no per-object property blocks, deliberately, so that it stays
    /// SRP Batcher compatible - animating a colour on it would set every outline in
    /// the büfe pulsing. This owns its own copies instead, which is cheap because
    /// only one object is ever highlighted.
    ///
    /// Two differences from the hover outline, and both of them are the point:
    ///   • It can draw through walls (ZTest Always), so a target behind the counter
    ///     or across the room still tells the player where to walk. The shader
    ///     already exposes this - see _ZTest in HoverOutline.shader.
    ///   • It stamps a different stencil bit, so a highlighted object and a hovered
    ///     object overlapping on screen do not carve holes in each other.
    ///
    /// Drop it on any always-present object (the Player, or a Systems object).
    /// </summary>
    [DisallowMultipleComponent]
    public class TargetHighlighter : MonoBehaviour
    {
        /// <summary>The live highlighter, or null when the scene has none.</summary>
        public static TargetHighlighter Instance { get; private set; }

        // Bit 2. The hover outline uses bit 3; URP reserves [0,3] for user code.
        private const float HighlightStencilBit = 4f;

        [Header("Look")]
        [Tooltip("Glow colour. HDR for bloom.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color glowColor = new Color(1f, 0.72f, 0.2f, 1f);

        [Tooltip("Min pulse thickness (px).")]
        [SerializeField] private float minWidth = 2.5f;

        [Tooltip("Max pulse thickness (px).")]
        [SerializeField] private float maxWidth = 6f;

        [Tooltip("Pulses per second. ~1 is calm.")]
        [SerializeField] private float pulsesPerSecond = 1.1f;

        [Tooltip("Dim at the thin end. 1 = no dimming.")]
        [Range(0f, 1f)]
        [SerializeField] private float dimAtThinnest = 0.55f;

        [Header("Behaviour")]
        [Tooltip("Draw through walls.")]
        [SerializeField] private bool seeThroughWalls = true;

        [Tooltip("Hide while hovered. Hover outline takes over.")]
        [SerializeField] private bool yieldToHover = true;

        [Tooltip("Hide once the target is held.")]
        [SerializeField] private bool hideWhileHeld = true;

        [Tooltip("Hover source. Auto-found.")]
        [SerializeField] private PlayerHand hand;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private static readonly int ColorId       = Shader.PropertyToID("_OutlineColor");
        private static readonly int WidthId       = Shader.PropertyToID("_OutlineWidth");
        private static readonly int ZTestId       = Shader.PropertyToID("_ZTest");
        private static readonly int StencilRefId  = Shader.PropertyToID("_StencilRef");
        private static readonly int StencilMaskId = Shader.PropertyToID("_StencilMask");

        private Material       _maskMaterial;
        private Material       _outlineMaterial;
        private GameObject     _target;
        private OutlineProxy[] _proxies;
        private bool           _drawing;

        /// <summary>The object currently being pointed at, or null.</summary>
        public GameObject Target => _target;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
            if (hand == null) hand = FindAnyObjectByType<PlayerHand>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            Clear();

            // Runtime copies, owned by nothing else - released by hand.
            if (_maskMaterial    != null) Destroy(_maskMaterial);
            if (_outlineMaterial != null) Destroy(_outlineMaterial);
        }

        // LateUpdate so PlayerHand has already resolved this frame's hover.
        private void LateUpdate()
        {
            if (_target == null)
            {
                // The target was destroyed under us (a runtime item that got used up).
                if (_proxies != null) Clear();
                return;
            }

            // Two reasons to go quiet, and both mean the glow has done its job:
            // the player is aiming at the target, or already carrying it.
            bool shouldDraw = !(yieldToHover  && IsHovered(_target))
                           && !(hideWhileHeld && IsHeld(_target));

            if (shouldDraw != _drawing)
            {
                _drawing = shouldDraw;
                OutlineHullBuilder.SetVisible(_proxies, shouldDraw);
            }

            if (shouldDraw) Pulse();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Points the glow at <paramref name="root"/> and everything under it,
        /// clearing whatever was highlighted before. Pass null to clear.
        /// </summary>
        public void SetTarget(GameObject root)
        {
            if (_target == root) return;

            Clear();

            _target = root;
            if (_target == null) return;

            EnsureMaterials();

            _proxies = OutlineHullBuilder.Build(_target, _maskMaterial, _outlineMaterial);
            _drawing = true;
            OutlineHullBuilder.SetVisible(_proxies, true);
            Pulse();
        }

        public void Clear()
        {
            OutlineHullBuilder.Destroy(_proxies);
            _proxies = null;
            _target  = null;
            _drawing = false;
        }

        // ── Internals ────────────────────────────────────────────────────────────

        private bool IsHeld(GameObject target)
        {
            InteractableItem held = hand != null ? hand.HeldItem : null;
            return held != null && SharesBranch(held.transform, target.transform);
        }

        private bool IsHovered(GameObject target)
        {
            GameObject hovered = hand != null ? hand.OutlinedRoot : null;
            if (hovered == null) return false;

            // The hand outlines whatever a click would act on, which may be a parent
            // of the highlighted object (a rack holding it) or a child of it (a
            // collider sitting under the shutter root). Either one counts as the
            // player looking at it.
            return SharesBranch(hovered.transform, target.transform);
        }

        /// <summary>
        /// True when the two transforms are the same object or one sits inside the
        /// other. Whatever the hand reports - a collider child, the root holding the
        /// script - counts as the highlighted thing as far as the player can tell.
        /// </summary>
        private static bool SharesBranch(Transform a, Transform b)
        {
            if (a == null || b == null) return false;
            return a == b || a.IsChildOf(b) || b.IsChildOf(a);
        }

        private void Pulse()
        {
            if (_outlineMaterial == null) return;

            // Unscaled time so the glow keeps breathing while a panel has paused the
            // game - the tutorial's own panels do exactly that.
            float phase = Time.unscaledTime * pulsesPerSecond * Mathf.PI * 2f;
            float t     = (Mathf.Sin(phase) + 1f) * 0.5f;

            _outlineMaterial.SetFloat(WidthId, Mathf.Lerp(minWidth, maxWidth, t));

            Color color = glowColor * Mathf.Lerp(dimAtThinnest, 1f, t);
            color.a = glowColor.a;
            _outlineMaterial.SetColor(ColorId, color);
        }

        private void EnsureMaterials()
        {
            if (_outlineMaterial != null) return;

            Material maskSource    = HoverOutlineService.MaskMaterial;
            Material outlineSource = HoverOutlineService.OutlineMaterial;

            if (maskSource == null || outlineSource == null)
            {
                Debug.LogError("[TargetHighlighter] The hover outline materials are missing, " +
                               "so there is nothing to copy the highlight from.", this);
                return;
            }

            // Copied from the hover materials rather than built from Shader.Find, so
            // the shaders are guaranteed to be in the build (Resources references
            // them) and every other property arrives already tuned.
            _maskMaterial    = new Material(maskSource)    { hideFlags = HideFlags.HideAndDontSave };
            _outlineMaterial = new Material(outlineSource) { hideFlags = HideFlags.HideAndDontSave };

            _maskMaterial.SetFloat(StencilRefId,  HighlightStencilBit);
            _maskMaterial.SetFloat(StencilMaskId, HighlightStencilBit);

            _outlineMaterial.SetFloat(StencilRefId,  HighlightStencilBit);
            _outlineMaterial.SetFloat(StencilMaskId, HighlightStencilBit);
            _outlineMaterial.SetFloat(ZTestId, (float)(seeThroughWalls
                ? CompareFunction.Always
                : CompareFunction.LessEqual));
        }
    }
}
