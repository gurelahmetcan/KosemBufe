using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vertical press-style minigame for the Toaster station.
/// Simulates pressing down a Turkish toaster machine.
///
/// Layout (all inside a vertical gauge track):
///   ┌─────────┐
///   │         │
///   │  🍞    │   ← toast icon (bounces up/down)
///   │         │
///   │ [=BAR=] │   ← player bar (moves up/down with W / S)
///   │         │
///   └─────────┘
///   [═ progress slider ═]
///
/// Controls during minigame:
///   W → move bar up     S → move bar down
///
/// Progress:
///   Toast inside bar  → slider fills  (toward 1)
///   Toast outside bar → slider depletes (toward 0)
///   Slider reaches 1  → success  (Final Toast)
///   Slider reaches 0  → failure  (Burned Toast)
///
/// Scene Setup:
///   1. Create a Screen Space – Overlay Canvas, start it disabled.
///   2. Inside it add:
///        gaugeTrack  – a vertical RectTransform (the track background image).
///        playerBar   – child RectTransform of gaugeTrack (the player bar image).
///        toastIcon   – child RectTransform of gaugeTrack (the toast icon image).
///        progressSlider – a UI Slider beside / below the track (yellow fill).
///   3. Assign PlayerCameraRotation so it can be paused during the minigame.
///   4. Assign this component's minigamePanel to the root Canvas GameObject.
/// </summary>
public class ToasterMinigame : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("PlayerCameraRotation component — disabled for the duration of the minigame.")]
    public PlayerCameraRotation cameraRotation;

    [Header("UI References")]
    [Tooltip("Root panel (Canvas or panel GameObject) shown during the minigame.")]
    public GameObject    minigamePanel;

    [Tooltip("The vertical track RectTransform that contains bar and icon.")]
    public RectTransform gaugeTrack;

    [Tooltip("The player-controlled bar RectTransform (child of gaugeTrack).")]
    public RectTransform playerBar;

    [Tooltip("The toast icon RectTransform that moves randomly (child of gaugeTrack).")]
    public RectTransform toastIcon;

    [Tooltip("Yellow progress Slider — value 0..1.")]
    public Slider progressSlider;

    [Header("Tuning")]
    [Tooltip("Speed (pixels/sec) the player bar moves with W / S.")]
    public float barSpeed = 400f;

    [Tooltip("Minimum speed (pixels/sec) of the toast icon.")]
    public float toastMinSpeed = 100f;

    [Tooltip("Maximum speed (pixels/sec) of the toast icon.")]
    public float toastMaxSpeed = 250f;

    [Tooltip("How fast the progress slider fills while toast is inside the bar (per second).")]
    public float fillRate = 0.3f;

    [Tooltip("How fast the progress slider depletes while toast is outside the bar (per second).")]
    public float depleteRate = 0.2f;

    [Tooltip("Starting progress value (0..1).")]
    [Range(0f, 1f)]
    public float startProgress = 0.5f;

    // ── Public state ──────────────────────────────────────────────────────────

    public bool IsActive { get; private set; }

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Action<bool> _onComplete;
    private float        _toastVelocity;
    private float        _dirChangeTimer;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Show the minigame UI and start the loop.
    /// onComplete(true) = success, onComplete(false) = burned.
    /// </summary>
    public void StartMinigame(Action<bool> onComplete)
    {
        _onComplete = onComplete;
        IsActive    = true;

        progressSlider.value       = startProgress;
        playerBar.anchoredPosition = Vector2.zero;
        toastIcon.anchoredPosition = Vector2.zero;
        _toastVelocity             = RandomSpeed();
        _dirChangeTimer            = UnityEngine.Random.Range(0.5f, 1.5f);

        if (cameraRotation) cameraRotation.enabled = false;
        minigamePanel.SetActive(true);
    }

    // ── Unity loop ────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!IsActive) return;

        MoveBar();
        MoveToast();
        UpdateProgress();
    }

    // ── Bar movement ──────────────────────────────────────────────────────────

    private void MoveBar()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.S)) input = -1f;
        if (Input.GetKey(KeyCode.W)) input =  1f;

        float halfTrack = gaugeTrack.rect.height * 0.5f;
        float halfBar   = playerBar.rect.height  * 0.5f;

        float y = playerBar.anchoredPosition.y + input * barSpeed * Time.deltaTime;
        y = Mathf.Clamp(y, -halfTrack + halfBar, halfTrack - halfBar);

        playerBar.anchoredPosition = new Vector2(playerBar.anchoredPosition.x, y);
    }

    // ── Toast icon movement ───────────────────────────────────────────────────

    private void MoveToast()
    {
        float halfTrack = gaugeTrack.rect.height * 0.5f;
        float halfIcon  = toastIcon.rect.height  * 0.5f;

        // Randomly change direction at intervals
        _dirChangeTimer -= Time.deltaTime;
        if (_dirChangeTimer <= 0f)
        {
            _toastVelocity  = UnityEngine.Random.value > 0.5f ? RandomSpeed() : -RandomSpeed();
            _dirChangeTimer = UnityEngine.Random.Range(0.4f, 1.8f);
        }

        float y = toastIcon.anchoredPosition.y + _toastVelocity * Time.deltaTime;

        // Bounce off track walls
        if (y > halfTrack - halfIcon)
        {
            y              =  halfTrack - halfIcon;
            _toastVelocity = -Mathf.Abs(_toastVelocity);
        }
        else if (y < -halfTrack + halfIcon)
        {
            y              = -halfTrack + halfIcon;
            _toastVelocity =  Mathf.Abs(_toastVelocity);
        }

        toastIcon.anchoredPosition = new Vector2(toastIcon.anchoredPosition.x, y);
    }

    // ── Progress ──────────────────────────────────────────────────────────────

    private bool IsToastInsideBar()
    {
        float toastY    = toastIcon.anchoredPosition.y;
        float barBottom = playerBar.anchoredPosition.y - playerBar.rect.height * 0.5f;
        float barTop    = playerBar.anchoredPosition.y + playerBar.rect.height * 0.5f;
        return toastY >= barBottom && toastY <= barTop;
    }

    private void UpdateProgress()
    {
        float delta = IsToastInsideBar() ? fillRate : -depleteRate;
        progressSlider.value += delta * Time.deltaTime;

        if      (progressSlider.value >= 1f) EndMinigame(true);
        else if (progressSlider.value <= 0f) EndMinigame(false);
    }

    // ── End ───────────────────────────────────────────────────────────────────

    private void EndMinigame(bool success)
    {
        IsActive = false;
        minigamePanel.SetActive(false);
        Debug.Log($"Minigame ended: {success}");

        if (cameraRotation) cameraRotation.enabled = true;

        _onComplete?.Invoke(success);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float RandomSpeed() =>
        UnityEngine.Random.Range(toastMinSpeed, toastMaxSpeed);
}
