using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toaster lid with a physical click-drag interaction.
///
/// The player left-click-holds this object and moves the mouse vertically
/// to push the lid down (close) or pull it up (open). Releasing mid-travel
/// snaps the lid to the nearest fully-open or fully-closed position.
///
/// Events:
///   OnLidClosed  — fired when the lid reaches the closed position.
///   OnLidOpened  — fired when the lid returns to the open position after being closed.
///
/// Setup:
///   1. Create a child GameObject on the Toaster with this component + a Collider.
///   2. Set openLocalY to the lid's starting local Y (usually 0).
///   3. Set closedLocalY to the local Y when fully pressed down (negative if lid moves down).
///   4. Wire OnLidClosed / OnLidOpened to Toaster (done in code via Subscribe()).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ToasterLid : MonoBehaviour, IAssemblyInteractable
{
    [Header("Travel")]
    [Tooltip("Local Y of the lid when fully open (rest position).")]
    public float openLocalY = 0f;

    [Tooltip("Local Y of the lid when fully closed (pressed down).")]
    public float closedLocalY = -0.15f;

    [Tooltip("World-units of lid movement per pixel of mouse movement.")]
    public float dragSensitivity = 0.003f;

    [Tooltip("Duration of the snap-to-end animation when the mouse is released.")]
    public float snapDuration = 0.15f;

    // ── Events ────────────────────────────────────────────────────────────────

    public event Action OnLidClosed;
    public event Action OnLidOpened;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private bool _isDragging;
    private bool _isLocked;

    private enum SnapState { Open, Closed }
    private SnapState _snapState = SnapState.Open;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// When locked the lid cannot be grabbed. Set by Toaster while cooking.
    /// </summary>
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

        // Positive mouse Y = moving up → lid opens (toward openLocalY).
        // Negative mouse Y = moving down → lid closes (toward closedLocalY).
        float delta = mouse.delta.y.ReadValue() * dragSensitivity;

        Vector3 pos = transform.localPosition;
        pos.y = Mathf.Clamp(pos.y + delta, closedLocalY, openLocalY);
        transform.localPosition = pos;

        CheckThresholds();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SnapToNearest()
    {
        float current = transform.localPosition.y;
        float mid     = (openLocalY + closedLocalY) * 0.5f;
        float targetY = current > mid ? openLocalY : closedLocalY;

        transform.DOLocalMoveY(targetY, snapDuration)
                 .SetEase(Ease.OutQuad)
                 .OnComplete(CheckThresholds);
    }

    private void CheckThresholds()
    {
        const float epsilon = 0.005f;
        float y = transform.localPosition.y;

        if (y <= closedLocalY + epsilon && _snapState == SnapState.Open)
        {
            _snapState = SnapState.Closed;
            OnLidClosed?.Invoke();
        }
        else if (y >= openLocalY - epsilon && _snapState == SnapState.Closed)
        {
            _snapState = SnapState.Open;
            OnLidOpened?.Invoke();
        }
    }
}
