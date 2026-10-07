using System;
using System.Collections;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// The shutter in front of the büfe. Clicking it opens the shop and starts the day.
    /// Attach to the shutter GameObject (needs a Collider for raycasting).
    /// </summary>
    public class Shutter : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [Header("References")]
        [Tooltip("Controls the day cycle.")]
        public DayManager dayManager;

        [Header("Hover Label")]
        [Tooltip("Label while it can be opened.")]
        [SerializeField] private string openLabel = "Open the Shutter";

        [Tooltip("Label once open. Empty = none.")]
        [SerializeField] private string openedLabel = "";

        public string HoverLabel => CanInteract(null) ? openLabel : openedLabel;

        [Header("Visuals")]
        [Tooltip("Plays the open clip forward, backward to close.")]
        public Animator animator;

        [Tooltip("Open state name (Loop Time off).")]
        public string animStateName = "ShutterAnim";

        [Tooltip("Close time. Match the open clip length.")]
        public float closeDuration = 1.2f;

        private Coroutine _closeRoutine;

        private void Awake()
        {
            // Start fully closed (first frame of the open clip), frozen until a phase change.
            if (animator != null)
            {
                animator.Play(animStateName, 0, 0f);
                animator.Update(0f);
                animator.speed = 0f;
            }
        }

        private void OnEnable()
        {
            if (dayManager != null)
                dayManager.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (dayManager != null)
                dayManager.OnPhaseChanged -= OnPhaseChanged;
        }

        public bool CanInteract(PlayerHand hand)
        {
            return dayManager != null && dayManager.CurrentPhase == DayManager.DayPhase.PreOpen;
        }

        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            dayManager.OpenShutter();   // → phase Open → OnPhaseChanged → OpenAnim() raises the shutter

            Log.Info("[Shutter] Büfe opened!");
        }

        /// <summary>Plays the open clip forward (0 → 1). SFX lives here so the prep-time
        /// auto-open sounds identical to a manual click.</summary>
        public void OpenAnim()
        {
            if (animator == null) return;
            if (_closeRoutine != null) { StopCoroutine(_closeRoutine); _closeRoutine = null; }

            if (AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Shutter);

            animator.speed = 1f;
            animator.Play(animStateName, 0, 0f);
        }

        /// <summary>
        /// Lowers the shutter - the reverse of opening. Unity forbids a negative Animator.speed
        /// at runtime, so instead of playing the clip backwards we scrub its normalized time
        /// from 1 → 0 ourselves (speed held at 0).
        /// </summary>
        public void CloseAnim()
        {
            if (animator == null) return;

            if (AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Shutter);

            if (_closeRoutine != null) StopCoroutine(_closeRoutine);
            _closeRoutine = StartCoroutine(ScrubClose());
        }

        private IEnumerator ScrubClose()
        {
            animator.speed = 0f;                     // we drive the playhead manually (never negative)
            float dur = Mathf.Max(0.0001f, closeDuration);
            float t   = 1f;

            while (t > 0f)
            {
                t -= Time.deltaTime / dur;
                animator.Play(animStateName, 0, Mathf.Clamp01(t));
                animator.Update(0f);                 // force it to sample the new pose this frame
                yield return null;
            }

            animator.Play(animStateName, 0, 0f);     // settle fully closed
            animator.Update(0f);
            _closeRoutine = null;
        }

        private void OnPhaseChanged(DayManager.DayPhase obj)
        {
            switch (obj)
            {
                case DayManager.DayPhase.PreOpen:
                    break;
                case DayManager.DayPhase.Open:
                    OpenAnim();
                    break;
                case DayManager.DayPhase.Closing:
                    break;
                case DayManager.DayPhase.EndOfDay:
                    CloseAnim();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }
    }
}
