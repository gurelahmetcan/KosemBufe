using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Bufe
{
    /// <summary>
    /// Procedural reach-and-grab for the right hand. When the player clicks an item,
    /// the right IK target swings from its rest pose toward the item, the pick-up
    /// runs at the END of the reach (so the item never floats to an empty hand), and
    /// the hand then returns to rest carrying the item — it is parented under the
    /// hand's HandDisplayPoint, so it follows on its own.
    ///
    /// The hand is moved through the same pivot HandBob uses (see <see cref="ArmsRig"/>):
    /// the right IK target is keyframed by the Idle/Grip clips, so writing to it
    /// directly would be overridden before the IK reads it.
    ///
    /// Distances are in world units: the item's world position is turned into a
    /// world offset, clamped to <see cref="maxReach"/> metres, and only then converted
    /// into the pivot's parent space — so the 0.6 / 0.07 / 1.67 scale factors in the
    /// arms hierarchy don't distort it.
    ///
    /// Storages use the point overload: the hand reaches to where the storage was
    /// clicked and the item is only spawned into the hand on arrival, so nothing
    /// floats or falls during the reach and a cancelled reach costs no stock.
    ///
    /// Robustness: this is presentation only. If anything is missing, TryReach
    /// returns false and the caller picks the item up immediately. If this component
    /// is disabled mid-reach, the pending pick-up still runs.
    ///
    /// Setup: put this on the object with the Animator + Rig Builder (next to
    /// HandBob). PlayerHand finds it automatically.
    /// </summary>
    public class HandReach : MonoBehaviour
    {
        [Header("IK Target")]
        [Tooltip("The right Two Bone IK constraint's Target. Empty = found automatically.")]
        [SerializeField] private Transform rightHandTarget;

        [Header("Timing")]
        [Tooltip("Seconds to reach from rest to the item (ease-in-out).")]
        [SerializeField, Min(0f)] private float reachDuration = 0.18f;

        [Tooltip("Seconds to bring the hand (and the item) back to rest (ease-in-out).")]
        [SerializeField, Min(0f)] private float returnDuration = 0.22f;

        [Header("Distance")]
        [Tooltip("Furthest the hand travels from its rest pose, in metres. Items further away " +
                 "are reached toward but not touched — the pick-up still happens.")]
        [SerializeField, Min(0f)] private float maxReach = 0.7f;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private Transform _pivot;
        private Vector3   _restLocal;
        private bool      _ready;

        private Coroutine _routine;
        private Action    _pendingGrab;

        /// <summary>
        /// True while the hand is being driven by a reach — the outward swing and the
        /// return to rest. HandBob stays off the hand for the whole time.
        /// </summary>
        public bool IsReaching => _routine != null;

        /// <summary>The Transform this component moves. HandBob compares against it.</summary>
        public Transform Pivot => _pivot;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            Animator animator = GetComponentInParent<Animator>();
            TwoBoneIKConstraint[] constraints = GetComponentsInChildren<TwoBoneIKConstraint>(true);

            Transform unusedLeft = null;
            ArmsRig.ResolveTargets(constraints, ref unusedLeft, ref rightHandTarget, this);

            bool addedMarker = false;
            _pivot = ArmsRig.ChoosePivot(rightHandTarget, animator, constraints, ref addedMarker);
            ArmsRig.RebuildIfNeeded(animator, addedMarker);

            if (_pivot == null) return;

            _restLocal = _pivot.localPosition;
            _ready     = true;
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            // Never swallow a pick-up because the animation was cut short.
            RunPendingGrab();

            if (_ready && _pivot != null)
                _pivot.localPosition = _restLocal;
        }

        // ── Public API (called by PlayerHand) ──────────────────────────────────────

        /// <summary>
        /// Starts reaching for <paramref name="item"/>; <paramref name="onGrab"/> runs when
        /// the hand arrives. Returns false — without calling onGrab — when the reach
        /// can't run, so the caller should pick the item up right away instead.
        /// </summary>
        public bool TryReach(InteractableItem item, Action onGrab)
        {
            if (item == null) return false;

            // Re-aimed every frame: the item may still be rolling or falling. Null once
            // it's gone (delivered, destroyed, reset) — nothing left to grab.
            return StartReach(() => item != null ? GrabPoint(item) : (Vector3?)null, onGrab);
        }

        /// <summary>
        /// Reaches toward a fixed world point (e.g. where a storage was clicked) and runs
        /// <paramref name="onGrab"/> on arrival. Same contract as the item overload.
        /// </summary>
        public bool TryReach(Vector3 worldPoint, Action onGrab)
            => StartReach(() => worldPoint, onGrab);

        private bool StartReach(Func<Vector3?> aim, Action onGrab)
        {
            if (!_ready || !isActiveAndEnabled || _pivot == null || rightHandTarget == null)
                return false;

            StopRoutine();              // a new reach takes over from wherever the hand is
            _pendingGrab = onGrab;
            _routine     = StartCoroutine(ReachRoutine(aim));
            return true;
        }

        /// <summary>
        /// Abandons the current reach: its pick-up is dropped and the hand heads back to
        /// rest from wherever it is. Called when a new click arrives mid-reach.
        /// </summary>
        public void Cancel()
        {
            if (_routine == null) return;

            StopRoutine();
            _pendingGrab = null;
            _routine     = StartCoroutine(ReturnRoutine());
        }

        // ── Motion ───────────────────────────────────────────────────────────────

        private IEnumerator ReachRoutine(Func<Vector3?> aim)
        {
            Vector3 from = _pivot.localPosition;
            float   t    = 0f;

            while (t < 1f)
            {
                Vector3? point = aim();

                // The thing being reached for is gone — nothing to grab.
                if (point == null)
                {
                    _pendingGrab = null;
                    break;
                }

                t += Time.deltaTime / Mathf.Max(0.0001f, reachDuration);

                _pivot.localPosition = Vector3.LerpUnclamped(from, GoalLocal(point.Value), Ease(t));
                yield return null;
            }

            RunPendingGrab();   // item snaps to the hand → PlayerHand switches Grip on

            yield return ReturnMotion();
            _routine = null;
        }

        private IEnumerator ReturnRoutine()
        {
            yield return ReturnMotion();
            _routine = null;
        }

        private IEnumerator ReturnMotion()
        {
            Vector3 from = _pivot.localPosition;
            float   t    = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, returnDuration);
                _pivot.localPosition = Vector3.LerpUnclamped(from, _restLocal, Ease(t));
                yield return null;
            }

            _pivot.localPosition = _restLocal;
        }

        /// <summary>
        /// Pivot local position that brings the IK target onto the item, limited to
        /// maxReach metres from rest. Everything is worked out in world space first.
        /// </summary>
        private Vector3 GoalLocal(Vector3 worldPoint)
        {
            // Moving the pivot moves the target by the same world offset (it rides on
            // top), so the pivot must travel exactly the target→point gap.
            Vector3 gap          = worldPoint - rightHandTarget.position;
            Vector3 desiredWorld = _pivot.position + gap;

            Transform parent    = _pivot.parent;
            Vector3   restWorld = parent != null ? parent.TransformPoint(_restLocal) : _restLocal;

            Vector3 goalWorld = restWorld + Vector3.ClampMagnitude(desiredWorld - restWorld, maxReach);

            return parent != null ? parent.InverseTransformPoint(goalWorld) : goalWorld;
        }

        /// <summary>
        /// Where on the item the hand aims: the centre of its collider bounds (an item's
        /// pivot often sits at its base), or its transform position when it has none.
        /// </summary>
        private static Vector3 GrabPoint(InteractableItem item)
        {
            bool   found  = false;
            Bounds bounds = default;

            foreach (Collider col in item.GetComponentsInChildren<Collider>())
            {
                if (!col.enabled) continue;
                if (!found) { bounds = col.bounds; found = true; }
                else        bounds.Encapsulate(col.bounds);
            }

            return found ? bounds.center : item.transform.position;
        }

        /// <summary>Smoothstep ease-in-out on 0–1.</summary>
        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void StopRoutine()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
        }

        /// <summary>
        /// Runs the pending pick-up exactly once. Guarded so an exception in it can't
        /// leave the reach stuck (IsReaching would otherwise stay true and freeze HandBob).
        /// </summary>
        private void RunPendingGrab()
        {
            Action grab = _pendingGrab;
            _pendingGrab = null;
            if (grab == null) return;

            try
            {
                grab();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
    }
}
