using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Generic hinged "openable" that rotates around a pivot when the player
/// click-drags it — a reusable version of the toaster-lid mechanism, meant to
/// be dropped onto fridge doors, cabinet doors, chest lids, oven hatches, etc.
///
/// The object starts CLOSED (its authored scene rotation). The player holds
/// left-click and drags the mouse to rotate it open around the hinge axis.
/// Releasing mid-travel snaps to the nearest fully-open or fully-closed pose.
///
/// Setup:
///   1. Give the moving part a PIVOT at the hinge edge (e.g. a fridge door's
///      pivot sits at its hinge side; a lid's pivot sits at its back edge).
///      The visible mesh should be a child offset so its hinge edge sits at
///      the pivot.
///   2. Add this component + a Collider to that GameObject.
///   3. Set rotationAxis to the hinge axis (Y for a swinging door, X for a lid).
///   4. Set openAngle to how far it opens (e.g. 90 for a door, 110 for a lid).
///   5. Set dragAxis to whichever mouse motion should drive it (Horizontal for
///      a swinging door, Vertical for a lid). Flip invertDrag if it opens the
///      wrong way.
///
/// Events:
///   OnClosed — fired when it returns to its fully-closed pose.
///   OnOpened — fired when it reaches the fully-open angle.
/// </summary>

namespace Bufe
{
    [RequireComponent(typeof(Collider))]
    public class HingedOpenable : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        public enum DragAxis { Vertical, Horizontal }

        [Header("Rotation")]
        [Tooltip("Degrees the object rotates from closed to fully open.")]
        public float openAngle = 90f;

        [Tooltip("Local axis the object rotates around. Y for a swinging door, X for a lid.")]
        public Vector3 rotationAxis = Vector3.up;

        [Header("Drag Input")]
        [Tooltip("Which mouse motion drives the opening: Horizontal (swinging door) or Vertical (lid).")]
        public DragAxis dragAxis = DragAxis.Horizontal;

        [Tooltip("Flip this if dragging opens the object the wrong way.")]
        public bool invertDrag = false;

        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        public float dragSensitivity = 0.5f;

        [Tooltip("Duration of the snap animation when the mouse is released.")]
        public float snapDuration = 0.2f;

        public string HoverLabel { get; }

        // ── Events ────────────────────────────────────────────────────────────────

        public event Action OnClosed;
        public event Action OnOpened;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private Quaternion _closedRotation;
        private float      _currentAngle;   // 0 = closed, openAngle = fully open

        private bool _isDragging;
        private bool _isLocked;

        private enum SnapState { Closed, Open }
        private SnapState _snapState = SnapState.Closed;

        // Registry of all openables so the day reset can snap them shut.
        private static readonly HashSet<HingedOpenable> _all = new HashSet<HingedOpenable>();

        protected virtual void Awake()
        {
            // Record the authored rotation as the closed position.
            // All rotations are applied as offsets from this baseline.
            _closedRotation = transform.localRotation;
            _currentAngle   = 0f;
        }

        protected virtual void OnEnable()  => _all.Add(this);
        protected virtual void OnDisable() => _all.Remove(this);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>True when the object is fully open.</summary>
        public bool IsOpen => _snapState == SnapState.Open;

        /// <summary>Locks/unlocks the object so it can't be dragged (e.g. while an appliance runs).</summary>
        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            if (locked && _isDragging)
                _isDragging = false;
        }

        /// <summary>
        /// Silently snaps the object shut and unlocks it — no OnClosed event fired.
        /// Used by the day reset so appliances (fridge, toaster lid) start each day closed.
        /// </summary>
        public void ForceClose()
        {
            _isDragging   = false;
            _isLocked     = false;
            _currentAngle = 0f;
            ApplyRotation();
            _snapState    = SnapState.Closed;
        }

        /// <summary>Snaps every openable shut. Called on the day reset.</summary>
        public static void ResetAllClosed()
        {
            foreach (var o in new List<HingedOpenable>(_all))
                if (o != null) o.ForceClose();
        }

        // Hooks for subclasses that want to rename/forward the open/close events
        // (e.g. ToasterLid exposing OnLidOpened/OnLidClosed) without duplicating logic.
        protected virtual void HandleOpened() { }
        protected virtual void HandleClosed() { }

        // ── IAssemblyInteractable ─────────────────────────────────────────────────

        public virtual bool CanInteract(PlayerHand hand)
        {
            return hand.IsHandEmpty && !_isLocked && !_isDragging;
        }

        public virtual void Interact(PlayerHand hand)
        {
            _isDragging = true;
        }

        // ── Drag update ───────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isDragging) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (!mouse.leftButton.isPressed)
            {
                _isDragging = false;
                SnapToNearest();
                return;
            }

            float raw = dragAxis == DragAxis.Vertical
                ? mouse.delta.y.ReadValue()
                : mouse.delta.x.ReadValue();

            if (invertDrag) raw = -raw;

            // Positive drag → angle increases (opens). Negative drag → closes.
            float delta = raw * dragSensitivity;
            _currentAngle = Mathf.Clamp(_currentAngle + delta, 0f, openAngle);

            ApplyRotation();
            CheckThresholds();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ApplyRotation()
        {
            transform.localRotation = _closedRotation * Quaternion.AngleAxis(_currentAngle, rotationAxis);
        }

        private void SnapToNearest()
        {
            float targetAngle = _currentAngle > openAngle * 0.5f ? openAngle : 0f;

            DOTween.To(
                () => _currentAngle,
                x  => { _currentAngle = x; ApplyRotation(); },
                targetAngle,
                snapDuration
            ).SetEase(Ease.OutQuad).OnComplete(CheckThresholds);
        }

        private void CheckThresholds()
        {
            const float epsilon = 1f; // degrees

            if (_currentAngle <= epsilon && _snapState == SnapState.Open)
            {
                _snapState = SnapState.Closed;
                HandleClosed();
                OnClosed?.Invoke();
            }
            else if (_currentAngle >= openAngle - epsilon && _snapState == SnapState.Closed)
            {
                _snapState = SnapState.Open;
                HandleOpened();
                OnOpened?.Invoke();
            }
        }
    }
}
