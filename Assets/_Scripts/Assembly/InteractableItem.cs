using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public class InteractableItem : MonoBehaviour
    {
        [Tooltip("Display name shown in the hand UI.")]
        public string itemName = "Item";
        
        // Euler angles rather than a Quaternion on purpose. The inspector draws a
        // Quaternion field as Euler angles, but it stores what you type as a
        // quaternion and converts back for display — and a quaternion has no memory
        // of which of the many Euler triples produced it. The drawer shows the
        // canonical one instead, so typing 150 into X can come back as 30 with Y and
        // Z flipped to 180-ish to compensate. Same rotation, unusable field. Storing
        // the angles means what is typed is what stays.
        [Tooltip("Rotation the item is held at, in degrees, relative to the hold point.")]
        public Vector3 InHandLocalEuler = Vector3.zero;

        [Tooltip("Offset from the hold point while held, in METRES — not in the hold " +
                 "point's local units. Rescaling the arm rig does not move it.")]
        public Vector3 InHandLocalPos = Vector3.zero;

        public Quaternion OriginalLocalRotation { get; private set; }

        /// <summary>
        /// The size the item was authored at, in world units, captured before
        /// anything re-parents it.
        ///
        /// Items get passed around a lot — hand, rack, prep station, toaster,
        /// sandwich socket — and every one of those anchors can carry a scale of its
        /// own. A local scale is only meaningful under the parent it was measured
        /// against, so restoring one under a different anchor resizes the item by
        /// whatever that anchor happens to be scaled to. A world scale means the same
        /// thing everywhere, so that is what gets remembered and restored.
        /// </summary>
        public Vector3 OriginalWorldScale { get; private set; }

        /// <summary>
        /// True for items the game has no second copy of — the knife, quest items. They
        /// are the ones the daily reset sends home instead of clearing, and the ones the
        /// trash bin refuses: destroying one leaves the player unable to prepare an order
        /// and no way to get it back.
        ///
        /// Read off the marker components that already say an item is irreplaceable
        /// (<see cref="ReturnHomeOnReset"/>, <see cref="QuestItem"/>) so there is no
        /// second flag to remember to tick on the prefab. Cached because the hover test
        /// runs every frame the crosshair is on the bin.
        /// </summary>
        public bool IsKeeper => _isKeeper;

        private bool _isKeeper;

        protected virtual void Awake()
        {
            OriginalLocalRotation = transform.localRotation;
            OriginalWorldScale    = transform.lossyScale;

            _isKeeper = GetComponent<ReturnHomeOnReset>() != null ||
                        GetComponent<QuestItem>()         != null;
        }

        /// <summary>
        /// Re-derives localScale so the item measures <see cref="OriginalWorldScale"/>
        /// in the world again, whatever it is parented to now. Call it after every
        /// re-parent; it is a no-op when the new parent is unscaled.
        /// </summary>
        public void RestoreWorldScale()
        {
            Vector3 parentScale = transform.parent != null
                ? transform.parent.lossyScale
                : Vector3.one;

            transform.localScale = ToLocalUnits(OriginalWorldScale, parentScale);
        }

        /// <summary>
        /// Parents the item to the hand and poses it there at its authored size.
        ///
        /// <see cref="InHandLocalPos"/> is applied in metres rather than in the hold
        /// point's own units. The hold point is a bone inside the arm rig and carries
        /// the rig's scale, so a local offset would mean a different real distance
        /// for every rig, and would shift the moment anyone rescaled one. Metres mean
        /// the same thing whatever the rig is scaled to.
        /// </summary>
        public void PlaceInHand(Transform holdPoint)
        {
            transform.SetParent(holdPoint);
            transform.localRotation = Quaternion.Euler(InHandLocalEuler);

            RestoreWorldScale();
            transform.localPosition = ToLocalUnits(InHandLocalPos, holdPoint.lossyScale);
        }

        /// <summary>
        /// Converts a world-space vector into the local units of a parent with the
        /// given scale. A zero-scaled parent has no size to divide by; the vector is
        /// passed through untouched, which keeps the item visible and makes the
        /// broken anchor obvious instead of collapsing the item along with it.
        /// </summary>
        private static Vector3 ToLocalUnits(Vector3 world, Vector3 parentScale)
        {
            return new Vector3(
                Mathf.Approximately(parentScale.x, 0f) ? world.x : world.x / parentScale.x,
                Mathf.Approximately(parentScale.y, 0f) ? world.y : world.y / parentScale.y,
                Mathf.Approximately(parentScale.z, 0f) ? world.z : world.z / parentScale.z);
        }

        public virtual void OnPickedUp() { }
        public virtual void OnDropped()  { }
    }
}
