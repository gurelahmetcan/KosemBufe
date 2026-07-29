using System.Collections;
using UnityEngine;

namespace Bufe.UI
{
    /// <summary>
    /// Full-screen fade-to-black helper. Put a CanvasGroup on a top-most,
    /// screen-space canvas whose only child is a solid black full-screen Image,
    /// attach this, and drive it with the FadeOut/FadeIn coroutines.
    ///
    /// Uses unscaled time so fades keep running even while the game is paused
    /// (Time.timeScale == 0), e.g. during menu/UI states.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [Tooltip("Seconds for a full fade in either direction.")]
        public float fadeDuration = 0.4f;

        [Tooltip("Start covered in solid black at scene load (no flash of the world). A controller " +
                 "— e.g. IntroController — decides when to fade in.")]
        public bool startBlack = false;

        private CanvasGroup _cg;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            _cg.alpha = startBlack ? 1f : 0f;
            _cg.blocksRaycasts = startBlack;
        }

        /// <summary>True while the screen is fully black (e.g. covering a load flash).</summary>
        public bool IsBlack => _cg != null && _cg.alpha >= 0.999f;

        /// <summary>Fade the screen to solid black.</summary>
        public IEnumerator FadeOut() => Fade(1f);

        /// <summary>Fade the screen back from black to clear.</summary>
        public IEnumerator FadeIn() => Fade(0f);

        private IEnumerator Fade(float target)
        {
            if (_cg == null) yield break;

            // Block clicks while (or as soon as) the screen is dark.
            if (target > 0.5f) _cg.blocksRaycasts = true;

            float start = _cg.alpha;
            float dur   = Mathf.Max(0.0001f, fadeDuration);
            float t     = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / dur;
                _cg.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }

            _cg.alpha = target;
            _cg.blocksRaycasts = target > 0.5f;
        }
    }
}
