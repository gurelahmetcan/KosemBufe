using System.Collections.Generic;
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

        private void Awake()
        {
            _homeParent = transform.parent;
            _startPos   = transform.position;
            _startRot   = transform.rotation;
        }

        private void OnEnable()  => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>Snaps this item back to its home pose (parent, position, rotation) and settles physics.</summary>
        public void ReturnHome()
        {
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
