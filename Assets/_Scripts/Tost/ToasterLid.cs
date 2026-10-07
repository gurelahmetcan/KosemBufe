using System;
using UnityEngine;

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
            // objects - which predate the dragAxis field - keep working.
            dragAxis = DragAxis.Vertical;
            base.Awake();
        }

        protected override void HandleClosed() => OnLidClosed?.Invoke();
        protected override void HandleOpened() => OnLidOpened?.Invoke();
    }
}
