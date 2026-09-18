using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bufe
{
    /// <summary>
    /// One line of "what you are supposed to be doing right now", parked near the top
    /// of the screen.
    ///
    /// Two sources feed it, and they never fight:
    ///
    ///   The tutorial (<see cref="TutorialDirector"/>) pushes an explicit line while
    ///     a guided step is running. That always wins.
    ///
    ///   Otherwise the day phase speaks for itself: PreOpen means the shutter is
    ///     still down, EndOfDay means the player can go home. Both are moments the
    ///     game used to announce only to the console, and both are moments a player
    ///     can sit in wondering what happened.
    ///
    /// Nothing here is a tutorial-only crutch: the phase lines keep working on day
    /// five, which is the point — the day-end door is no more discoverable on day
    /// five than it is on day one.
    ///
    /// Built in code like <see cref="InteractionHintUI"/>, with no GraphicRaycaster,
    /// so it can never swallow a click meant for the game or for another panel.
    /// Drop it on any always-present object.
    /// </summary>
    public class ObjectiveUI : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Day manager whose phase drives the fallback lines. Auto-found if left empty.")]
        [SerializeField] private DayManager dayManager;

        [Tooltip("Font for the line. Falls back to the TMP default.")]
        [SerializeField] private TMP_FontAsset font;

        [Header("Phase Lines")]
        [Tooltip("Shown while the shutter is still down. Empty = show nothing in this phase.")]
        [SerializeField] private string preOpenLine = "Open the shutter to start the day";

        [Tooltip("Shown while the shop is open and customers are coming. Usually empty — " +
                 "the player has plenty to look at without a line nagging them.")]
        [SerializeField] private string openLine = "";

        [Tooltip("Shown after closing time while the last customer is being served.")]
        [SerializeField] private string closingLine = "Serve the last customer";

        [Tooltip("Shown once the day can be ended.")]
        [SerializeField] private string endOfDayLine = "Head out the door and go home";

        [Header("Placement")]
        [Tooltip("Distance from the top edge, in reference pixels.")]
        [SerializeField] private float topMargin = 96f;

        [Header("Appearance")]
        [SerializeField] private float fontSize  = 30f;
        [SerializeField] private Color textColor = new Color(1f, 0.93f, 0.78f, 1f);

        [Tooltip("Dark plate behind the text, so the line stays readable against a bright wall.")]
        [SerializeField] private Color plateColor = new Color(0f, 0f, 0f, 0.45f);

        [Tooltip("Seconds the line takes to fade in or out when it changes.")]
        [SerializeField] private float fadeDuration = 0.25f;

        [Tooltip("Canvas sorting order. Raise it if another UI covers the line.")]
        [SerializeField] private int sortingOrder = 45;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private CanvasGroup _group;
        private TMP_Text    _text;
        private string      _shown;          // what the label currently reads
        private string      _override;       // line pushed by the tutorial, null when it has none
        private float       _alpha;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (dayManager == null) dayManager = FindAnyObjectByType<DayManager>();

            Build();
        }

        private void LateUpdate()
        {
            string wanted = Resolve();

            // Swap the words only while the plate is invisible, so a change reads as
            // "one line fades out, the next fades in" instead of the text mutating
            // under the player's eye mid-fade.
            if (wanted != _shown && _alpha <= 0f)
            {
                _shown = wanted;
                _text.text = wanted ?? string.Empty;
            }

            bool wantVisible = !string.IsNullOrEmpty(_shown) && _shown == wanted && Visible();

            float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;
            _alpha = Mathf.MoveTowards(_alpha, wantVisible ? 1f : 0f, step);

            if (!Mathf.Approximately(_group.alpha, _alpha))
                _group.alpha = _alpha;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Shows <paramref name="line"/> instead of the phase line. Pass null or an
        /// empty string to hand the line back to the day phase.
        /// </summary>
        public void SetObjective(string line)
        {
            _override = string.IsNullOrEmpty(line) ? null : line;
        }

        public void ClearObjective() => SetObjective(null);

        // ── Internals ────────────────────────────────────────────────────────────

        private string Resolve()
        {
            if (_override != null) return _override;
            if (dayManager == null) return null;

            return dayManager.CurrentPhase switch
            {
                DayManager.DayPhase.PreOpen  => preOpenLine,
                DayManager.DayPhase.Open     => openLine,
                DayManager.DayPhase.Closing  => closingLine,
                DayManager.DayPhase.EndOfDay => endOfDayLine,
                _                            => null
            };
        }

        /// <summary>
        /// A free cursor means a panel owns the screen — the intro conversation, the
        /// order panel, the pause menu. The objective is about the world, so it gets
        /// out of the way until the player is back in it.
        /// </summary>
        private static bool Visible() => Cursor.lockState == CursorLockMode.Locked;

        private void Build()
        {
            var canvasObject = new GameObject("ObjectiveCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            // No GraphicRaycaster on purpose — see the class summary.

            var plateObject = new GameObject("Plate", typeof(RectTransform));
            plateObject.transform.SetParent(canvasObject.transform, false);

            var rect = (RectTransform)plateObject.transform;
            rect.anchorMin        = new Vector2(0.5f, 1f);
            rect.anchorMax        = new Vector2(0.5f, 1f);
            rect.pivot            = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topMargin);

            _group = plateObject.AddComponent<CanvasGroup>();
            _group.alpha          = 0f;
            _group.interactable   = false;
            _group.blocksRaycasts = false;

            var plate = plateObject.AddComponent<Image>();
            plate.color         = plateColor;
            plate.raycastTarget = false;

            var layout = plateObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding                = new RectOffset(26, 26, 8, 10);
            layout.childAlignment         = TextAnchor.MiddleCenter;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;

            var fitter = plateObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var textObject = new GameObject("Line", typeof(RectTransform));
            textObject.transform.SetParent(plateObject.transform, false);

            _text = textObject.AddComponent<TextMeshProUGUI>();
            if (font != null) _text.font = font;

            _text.fontSize         = fontSize;
            _text.color            = textColor;
            _text.alignment        = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.NoWrap;   // so preferred width is the whole string
            _text.raycastTarget    = false;
            _text.text             = string.Empty;
        }
    }
}
