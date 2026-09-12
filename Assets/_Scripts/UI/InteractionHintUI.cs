using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bufe
{
    /// <summary>
    /// On-screen input hints: one "[key] : action" row per thing the player can
    /// actually do right now.
    ///
    /// The rows are split across two places on purpose, because they answer two
    /// different questions:
    ///
    ///   Bottom centre — AIM hints. "[LMB] : Bread", "[RMB] : Take Back". These
    ///     depend on what the crosshair is on, so they belong where the player is
    ///     already looking, and they come and go constantly.
    ///
    ///   Bottom right — HAND hints. "[RMB] : Drop Bread", "[HOLD RMB] : Throw".
    ///     These depend only on what is being carried and stay put while it is
    ///     carried. Parking them in a corner keeps the centre of the screen from
    ///     jumping between one and three rows every time the player looks around
    ///     with something in hand.
    ///
    /// Nothing here is a manual: a row is only ever drawn when pressing that key
    /// would do something. The state comes from <see cref="PlayerHand"/> — the same
    /// resolution that decides what gets an outline — so the outline, the hint and
    /// the click can never disagree.
    ///
    /// Setup: drop this on any object in the scene (the Player is the obvious home)
    /// and it builds its own overlay canvas at runtime. It deliberately does NOT add
    /// a GraphicRaycaster, so it can never swallow a click meant for the game or for
    /// another UI.
    ///
    /// Note: this replaces PlayerHand's "hand status text" — the rows already name
    /// the object being looked at and the item being held. Clear that field on
    /// PlayerHand, or the same words appear twice.
    /// </summary>
    public class InteractionHintUI : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Hand to read the interaction state from. Auto-found if left empty.")]
        [SerializeField] private PlayerHand hand;

        [Tooltip("Font for the hints. Falls back to the hand status text's font, then to the TMP default.")]
        [SerializeField] private TMP_FontAsset font;

        [Header("Keys")]
        [SerializeField] private string primaryKey   = "LMB";
        [SerializeField] private string secondaryKey = "RMB";
        [SerializeField] private string holdKey      = "HOLD RMB";

        [Header("Actions")]
        [Tooltip("Empty-hand right-click on something that can hand an item back (e.g. the prep station).")]
        [SerializeField] private string takeBackLabel = "Take Back";

        [SerializeField] private string dropLabel  = "Drop";
        [SerializeField] private string throwLabel = "Throw";

        [Tooltip("Append the carried item's name to the drop row, e.g. \"Drop Bread\". " +
                 "Turn this off for a plain \"Drop\".")]
        [SerializeField] private bool showHeldItemName = true;

        [Tooltip("Show the hold-to-throw row while carrying something.")]
        [SerializeField] private bool showThrowHint = true;

        [Tooltip("Printed between the key badge and the action. Leave empty for no separator.")]
        [SerializeField] private string separator = ":";

        [Header("Placement")]
        [Tooltip("Park the carry hints (drop/throw) in the bottom-right corner. " +
                 "Turn this off to stack every hint at the bottom centre instead.")]
        [SerializeField] private bool handHintsInCorner = true;

        [Tooltip("Distance from the bottom edge for the centre hints, in reference pixels.")]
        [SerializeField] private float centreMargin = 48f;

        [Tooltip("Distance from the bottom-right corner for the carry hints, in reference pixels.")]
        [SerializeField] private Vector2 cornerMargin = new Vector2(32f, 28f);

        [Header("Appearance")]
        [SerializeField] private float fontSize   = 26f;
        [SerializeField] private float rowSpacing = 6f;

        [SerializeField] private Color textColor      = Color.white;
        [SerializeField] private Color badgeColor     = new Color(0.93f, 0.93f, 0.95f, 0.92f);
        [SerializeField] private Color badgeTextColor = new Color(0.08f, 0.08f, 0.10f, 1f);

        [Tooltip("Dark rim around the text, so hints stay readable against a bright wall or a lamp.")]
        [SerializeField, Range(0f, 0.5f)] private float textOutline = 0.2f;

        [Tooltip("Canvas sorting order. Raise it if another UI covers the hints.")]
        [SerializeField] private int sortingOrder = 50;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private sealed class Row
        {
            public GameObject Root;
            public TMP_Text   Key;
            public TMP_Text   Label;

            // Last values pushed, so an unchanged frame touches no TMP state and
            // triggers no layout rebuild.
            public string KeyText;
            public string LabelText;
        }

        private sealed class HintGroup
        {
            public RectTransform     Root;
            public TextAnchor        RowAlignment;
            public readonly List<Row> Rows = new List<Row>(2);
            public int               Used;
        }

        private HintGroup _centre;
        private HintGroup _corner;

        private Sprite        _badgeSprite;
        private TMP_FontAsset _resolvedFont;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (hand == null) hand = FindAnyObjectByType<PlayerHand>();

            _resolvedFont = font;
            if (_resolvedFont == null && hand != null && hand.handStatusText != null)
                _resolvedFont = hand.handStatusText.font;

            Build();
        }

        private void OnDestroy()
        {
            if (_badgeSprite == null) return;

            // Generated at runtime and not owned by the asset database, so the
            // texture has to be released explicitly along with the sprite.
            Texture texture = _badgeSprite.texture;
            Destroy(_badgeSprite);
            if (texture != null) Destroy(texture);
        }

        // LateUpdate so PlayerHand has already resolved this frame's hover.
        private void LateUpdate()
        {
            _centre.Used = 0;
            _corner.Used = 0;

            // A free cursor means a menu is open; InputBlocked means a station is
            // driving the player. Neither state accepts the presses shown here.
            bool active = hand != null
                       && !hand.InputBlocked
                       && Cursor.lockState == CursorLockMode.Locked;

            if (active)
            {
                if (hand.HasAction && !string.IsNullOrEmpty(hand.HoverLabel))
                    Add(_centre, primaryKey, hand.HoverLabel);

                if (hand.IsHandEmpty)
                {
                    if (hand.HasSecondaryAction)
                        Add(_centre, secondaryKey, takeBackLabel);
                }
                else
                {
                    HintGroup carry = handHintsInCorner ? _corner : _centre;

                    // Naming the item keeps the player's inventory readable now that
                    // the old status line is gone.
                    string drop = showHeldItemName && hand.HeldItem != null
                        ? $"{dropLabel} {hand.HeldItem.itemName}"
                        : dropLabel;

                    Add(carry, secondaryKey, drop);

                    if (showThrowHint)
                        Add(carry, holdKey, throwLabel);
                }
            }

            Flush(_centre);
            Flush(_corner);
        }

        // ── Row management ───────────────────────────────────────────────────────

        private void Add(HintGroup group, string key, string action)
        {
            Row row = GetRow(group, group.Used);

            if (row.KeyText != key)
            {
                row.KeyText  = key;
                row.Key.text = key;
            }

            string label = string.IsNullOrEmpty(separator) ? action : $"{separator} {action}";

            if (row.LabelText != label)
            {
                row.LabelText  = label;
                row.Label.text = label;
            }

            group.Used++;
        }

        private static void Flush(HintGroup group)
        {
            for (int i = 0; i < group.Rows.Count; i++)
            {
                bool active = i < group.Used;
                if (group.Rows[i].Root.activeSelf != active)
                    group.Rows[i].Root.SetActive(active);
            }
        }

        private Row GetRow(HintGroup group, int index)
        {
            while (group.Rows.Count <= index)
                group.Rows.Add(BuildRow(group));

            return group.Rows[index];
        }

        // ── Construction ─────────────────────────────────────────────────────────

        private void Build()
        {
            var canvasObject = new GameObject("InteractionHintCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            // Screen space overlay ignores its transform, so parenting to the player
            // is purely for lifetime: the hints die with whatever owns them.
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            // No GraphicRaycaster on purpose — see the class summary.

            _badgeSprite = BuildBadgeSprite();

            _centre = BuildGroup(canvasObject.transform, "CentreHints",
                                 anchor: new Vector2(0.5f, 0f),
                                 pivot:  new Vector2(0.5f, 0f),
                                 offset: new Vector2(0f, centreMargin),
                                 // Rows centred under the crosshair.
                                 blockAlignment: TextAnchor.LowerCenter,
                                 rowAlignment:   TextAnchor.MiddleLeft);

            _corner = BuildGroup(canvasObject.transform, "CarryHints",
                                 anchor: new Vector2(1f, 0f),
                                 pivot:  new Vector2(1f, 0f),
                                 offset: new Vector2(-cornerMargin.x, cornerMargin.y),
                                 // Badges line up down the left edge of the block, so
                                 // the eye reads one column of keys, not a ragged edge.
                                 blockAlignment: TextAnchor.LowerLeft,
                                 rowAlignment:   TextAnchor.MiddleLeft);
        }

        private HintGroup BuildGroup(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                                     Vector2 offset, TextAnchor blockAlignment, TextAnchor rowAlignment)
        {
            var groupObject = new GameObject(name, typeof(RectTransform));
            groupObject.transform.SetParent(parent, false);

            var rect = (RectTransform)groupObject.transform;
            rect.anchorMin        = anchor;
            rect.anchorMax        = anchor;
            rect.pivot            = pivot;
            rect.anchoredPosition = offset;

            var layout = groupObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing                = rowSpacing;
            layout.childAlignment         = blockAlignment;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;

            // Only the group root needs a fitter: every level below is sized by the
            // layout group above it, which reads the preferred size that level reports.
            var fitter = groupObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            return new HintGroup { Root = rect, RowAlignment = rowAlignment };
        }

        private Row BuildRow(HintGroup group)
        {
            var rowObject = new GameObject("Hint", typeof(RectTransform));
            rowObject.transform.SetParent(group.Root, false);

            var rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing                = 10f;
            rowLayout.childAlignment         = group.RowAlignment;
            rowLayout.childControlWidth      = true;
            rowLayout.childControlHeight     = true;
            rowLayout.childForceExpandWidth  = false;
            rowLayout.childForceExpandHeight = false;

            // ── Key badge ───────────────────────────────────────────────────────
            var badgeObject = new GameObject("Badge", typeof(RectTransform));
            badgeObject.transform.SetParent(rowObject.transform, false);

            var badge = badgeObject.AddComponent<Image>();
            badge.sprite        = _badgeSprite;
            badge.type          = Image.Type.Sliced;
            badge.color         = badgeColor;
            badge.raycastTarget = false;

            var badgeLayout = badgeObject.AddComponent<HorizontalLayoutGroup>();
            badgeLayout.padding                = new RectOffset(10, 10, 3, 3);
            badgeLayout.childAlignment         = TextAnchor.MiddleCenter;
            badgeLayout.childControlWidth      = true;
            badgeLayout.childControlHeight     = true;
            badgeLayout.childForceExpandWidth  = false;
            badgeLayout.childForceExpandHeight = false;

            // Keeps a one-letter key square instead of a thin sliver.
            var badgeSize = badgeObject.AddComponent<LayoutElement>();
            badgeSize.minWidth  = fontSize * 1.6f;
            badgeSize.minHeight = fontSize * 1.6f;

            TMP_Text keyText = BuildText("Key", badgeObject.transform, badgeTextColor,
                                         TextAlignmentOptions.Center);

            // ── Action label ────────────────────────────────────────────────────
            TMP_Text labelText = BuildText("Label", rowObject.transform, textColor,
                                           TextAlignmentOptions.MidlineLeft);

            rowObject.SetActive(false);

            return new Row { Root = rowObject, Key = keyText, Label = labelText };
        }

        private TMP_Text BuildText(string name, Transform parent, Color color, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<TextMeshProUGUI>();
            if (_resolvedFont != null) text.font = _resolvedFont;

            text.fontSize         = fontSize;
            text.color            = color;
            text.alignment        = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;   // so preferred width is the whole string
            text.raycastTarget    = false;

            if (textOutline > 0f)
            {
                // Touching fontMaterial first forces a per-object material instance.
                // Without it, TMP writes the outline into the font's SHARED material
                // and every other label in the game gets a black rim too.
                _ = text.fontMaterial;
                text.outlineColor = Color.black;
                text.outlineWidth = textOutline;
            }

            return text;
        }

        /// <summary>
        /// A white rounded rectangle, generated rather than shipped as an asset so
        /// there is nothing to wire up. Nine-sliced on the corner radius, so one
        /// small texture stretches cleanly behind keys of any width.
        /// </summary>
        private static Sprite BuildBadgeSprite()
        {
            const int size   = 32;
            const int radius = 10;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
                name       = "HintBadge"
            };

            var pixels = new Color32[size * size];
            const float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to a rounded rectangle: shrink the box by the
                    // corner radius, measure to that, then subtract the radius back.
                    float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float py = Mathf.Abs(y + 0.5f - half) - (half - radius);

                    float qx = Mathf.Max(px, 0f);
                    float qy = Mathf.Max(py, 0f);

                    float distance = Mathf.Sqrt(qx * qx + qy * qy) - radius;

                    byte alpha = (byte)(Mathf.Clamp01(0.5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture,
                                 new Rect(0f, 0f, size, size),
                                 new Vector2(0.5f, 0.5f),
                                 100f,
                                 0,
                                 SpriteMeshType.FullRect,
                                 new Vector4(radius, radius, radius, radius));
        }
    }
}
