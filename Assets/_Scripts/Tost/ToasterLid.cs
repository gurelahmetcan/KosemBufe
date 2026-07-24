using System;
using UnityEngine;

/// <summary>
/// Toaster lid — a thin specialisation of the generic <see cref="HingedOpenable"/>
/// that rotates open around a hinge pivot.
///
/// The lid starts CLOSED (its authored scene rotation). The player holds
/// left-click and drags the mouse upward to rotate the lid open around the
/// hinge axis. Releasing mid-travel snaps to the nearest fully-open or
/// fully-closed position.
///
/// All of the drag/rotate/snap behaviour lives in HingedOpenable; this class
/// only re-exposes the lid-flavoured event names that Toaster subscribes to and
/// ships lid-appropriate default values (hinge along X, vertical drag).
///
/// Setup:
///   1. Create the Lid GameObject with its PIVOT at the hinge edge (back of toaster).
///      The lid mesh should be a child offset so its hinge edge sits at the pivot.
///   2. Add this component + a Collider to the Lid GameObject.
///   3. Set openAngle to how many degrees the lid rotates open (e.g. 110).
///   4. Assign to Toaster.lid in the Inspector.
///
/// Events:
///   OnLidClosed — fired when the lid rotates back to its fully-closed position.
///   OnLidOpened — fired when the lid reaches the fully-open angle.
/// </summary>

namespace Bufe
{
    public class ToasterLid : HingedOpenable
    {
        // ── Lid-flavoured event aliases (kept for Toaster + existing subscribers) ──

        public event Action OnLidClosed;
        public event Action OnLidOpened;

        protected override void Awake()
        {
            // A lid is always opened by dragging the mouse UP, regardless of the
            // (new) generic dragAxis default. Force it so existing toaster scene
            // objects — which predate the dragAxis field — keep working.
            dragAxis = DragAxis.Vertical;
            base.Awake();
        }

        protected override void HandleClosed() => OnLidClosed?.Invoke();
        protected override void HandleOpened() => OnLidOpened?.Invoke();
    }
}
