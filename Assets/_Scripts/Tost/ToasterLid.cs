using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toaster lid that rotates open around a hinge pivot.
///
/// The lid starts CLOSED (its authored scene rotation). The player holds
/// left-click and drags the mouse upward to rotate the lid open around the
/// hinge axis. Releasing mid-travel snaps to the nearest fully-open or
/// fully-closed position.
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
[RequireComponent(typeof(Collider))]
public class ToasterLid : MonoBehaviour, IAssemblyInteractable
{
    [Header("Rotation")]
    [Tooltip("Degrees the lid rotates from closed to fully open.")]
    public float openAngle = 110f;

    [Tooltip("Local axis the lid rotates around. Default is X (hinge along left-right).")]
    public Vector3 rotationAxis = Vector3.right;

    [Tooltip("Degrees of rotation per pixel of upward mouse movement.")]
    public float dragSensitivity = 0.5f;

    [Tooltip("Duration of the snap animation when the mouse is released.")]
    public float snapDuration = 0.2f;

    // ── Events ────────────────────────────────────────────────────────────────

    public event Action OnLidClosed;
    public event Action OnLidOpened;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private Quaternion _closedRotation;
    private float      _currentAngle;   // 0 = closed, openAngle = fully open

    private bool _isDragging;
    private bool _isLocked;

    private enum SnapState { Closed, Open }
    private SnapState _snapState = SnapState.Closed;

    private void Awake()
    {
        // Record the authored rotation as the closed position.
        // All rotations are applied as offsets from this baseline.
        _closedRotation = transform.localRotation;
        _currentAngle   = 0f;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>True when the lid is fully open.</summary>
    public bool IsOpen => _snapState == SnapState.Open;

    /// <summary>Locks/unlocks the lid. Called by Toaster during cooking.</summary>
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
        if (locked && _isDragging)
            _isDragging = false;
    }

    // ── IAssemblyInteractable ─────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        return hand.IsHandEmpty && !_isLocked && !_isDragging;
    }

    public void Interact(PlayerHand hand)
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

        // Mouse up → angle increases (lid opens). Mouse down → angle decreases (lid closes).
        float delta = mouse.delta.y.ReadValue() * dragSensitivity;
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
            OnLidClosed?.Invoke();
        }
        else if (_currentAngle >= openAngle - epsilon && _snapState == SnapState.Closed)
        {
            _snapState = SnapState.Open;
            OnLidOpened?.Invoke();
        }
    }
}
