using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Marks an important item (e.g. the knife) that must return to its start pose at the
    /// beginning of each day instead of being lost wherever it was left. On the day reset,
    /// DayResetManager calls <see cref="ReturnAllHome"/>, snapping each tagged item back to
    /// its home. Point <see cref="homePoint"/> at a transform to use as home, or leave it
    /// empty to use the item's authored scene pose.
    /// </summary>
    public class ReturnHomeOnReset : MonoBehaviour
    {
        [Tooltip("Where the item returns to. Empty = its position/rotation at scene start.")]
        [SerializeField] private Transform homePoint;

        private static readonly HashSet<ReturnHomeOnReset> _all = new HashSet<ReturnHomeOnReset>();

        private Vector3    _startPos;
        private Quaternion _startRot;
        private Transform  _homeParent;
        private Sequence   _flight;      // in-flight FlyHome tween, null when settled

        private void Awake()
        {
            _homeParent = transform.parent;
            _startPos   = transform.position;
            _startRot   = transform.rotation;
        }

        private void OnEnable()  => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>Where this item belongs: its home point if one is wired, else its scene-start pose.</summary>
        public Vector3    HomePosition => homePoint != null ? homePoint.position : _startPos;
        public Quaternion HomeRotation => homePoint != null ? homePoint.rotation : _startRot;

        /// <summary>
        /// The animated version of <see cref="ReturnHome"/>: the item arcs back to its
        /// home pose over <paramref name="duration"/> instead of teleporting.
        ///
        /// Used when an item finishes its job mid-play — the knife after a cut. The
        /// snap version stays for the day reset, which happens behind a black screen
        /// where an arc would be seen by nobody.
        ///
        /// Physics and colliders are off for the flight and restored on landing, so
        /// nothing can be knocked about by a tool travelling across the counter, and
        /// the item cannot be grabbed out of the air mid-flight.
        /// </summary>
        public void FlyHome(float duration)
        {
            if (duration <= 0f)
            {
                ReturnHome();
                return;
            }

            if (_flight != null && _flight.IsActive()) _flight.Kill();

            transform.SetParent(_homeParent, worldPositionStays: true);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic     = true;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                col.enabled = false;

            Sequence flight = DOTween.Sequence();
            flight.Join(transform.DOMove(HomePosition, duration).SetEase(Ease.InOutQuad));
            flight.Join(transform.DORotateQuaternion(HomeRotation, duration).SetEase(Ease.InOutQuad));

            // A tween outlives the object it moves unless it is told not to, and this
            // one runs while the player is free to trigger a day reset.
            flight.SetTarget(transform);
            flight.SetLink(gameObject);

            flight.OnComplete(() =>
            {
                foreach (Collider col in colliders)
                    if (col != null) col.enabled = true;

                if (rb != null) rb.isKinematic = false;
                _flight = null;
            });

            _flight = flight;
        }

        /// <summary>Snaps this item back to its home pose (parent, position, rotation) and settles physics.</summary>
        public void ReturnHome()
        {
            // A flight still in progress would keep writing the transform after this snap.
            if (_flight != null && _flight.IsActive()) { _flight.Kill(); _flight = null; }

            Vector3    pos = homePoint != null ? homePoint.position : _startPos;
            Quaternion rot = homePoint != null ? homePoint.rotation : _startRot;

            transform.SetParent(_homeParent, worldPositionStays: true);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(pos, rot);

            // Colliders may have been disabled while the item was held — re-enable them.
            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = true;
        }

        /// <summary>Returns every tagged item to its home pose. Called on the day reset.</summary>
        public static void ReturnAllHome()
        {
            foreach (var item in new List<ReturnHomeOnReset>(_all))
                if (item != null) item.ReturnHome();
        }
    }
}
