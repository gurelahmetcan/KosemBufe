using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horizontal Stardew-style minigame for the Toaster station.
///
/// Layout (all inside a horizontal gauge track):
///   ┌──────────────────────────────────────────┐
///   │        [==BAR==]        🍞               │   ← gauge track
///   └──────────────────────────────────────────┘
///   [════════ progress slider ════════]
///
/// Controls during minigame:
///   A → move bar left     D → move bar right
///
/// Progress:
///   Toast inside bar  → yellow slider fills  (toward 1)
///   Toast outside bar → yellow slider depletes (toward 0)
///   Slider reaches 1  → success  (Final Toast)
///   Slider reaches 0  → failure  (Burned Toast)
///
/// Scene Setup:
///   1. Create a Screen Space – Overlay Canvas, start it disabled.
///   2. Inside it add:
///        gaugeTrack  – a horizontal RectTransform (the track background image).
///        playerBar   – child RectTransform of gaugeTrack (the player bar image).
///        toastIcon   – child RectTransform of gaugeTrack (the toast icon image).
///        progressSlider – a UI Slider below / above the track (yellow fill).
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

    [Tooltip("The horizontal track RectTransform that contains bar and icon.")]
    public RectTransform gaugeTrack;

    [Tooltip("The player-controlled bar RectTransform (child of gaugeTrack).")]
    public RectTransform playerBar;

    [Tooltip("The toast icon RectTransform that moves randomly (child of gaugeTrack).")]
    public RectTransform toastIcon;

    [Tooltip("Yellow progress Slider — value 0..1.")]
    public Slider progressSlider;

    [Header("Tuning")]
    [Tooltip("Speed (pixels/sec) the player bar moves with A / D.")]
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
        if (Input.GetKey(KeyCode.A)) input = -1f;
        if (Input.GetKey(KeyCode.D)) input =  1f;

        float halfTrack = gaugeTrack.rect.width * 0.5f;
        float halfBar   = playerBar.rect.width  * 0.5f;

        float x = playerBar.anchoredPosition.x + input * barSpeed * Time.deltaTime;
        x = Mathf.Clamp(x, -halfTrack + halfBar, halfTrack - halfBar);

        playerBar.anchoredPosition = new Vector2(x, playerBar.anchoredPosition.y);
    }

    // ── Toast icon movement ───────────────────────────────────────────────────

    private void MoveToast()
    {
        float halfTrack = gaugeTrack.rect.width * 0.5f;
        float halfIcon  = toastIcon.rect.width  * 0.5f;

        // Randomly change direction at intervals
        _dirChangeTimer -= Time.deltaTime;
        if (_dirChangeTimer <= 0f)
        {
            _toastVelocity  = UnityEngine.Random.value > 0.5f ? RandomSpeed() : -RandomSpeed();
            _dirChangeTimer = UnityEngine.Random.Range(0.4f, 1.8f);
        }

        float x = toastIcon.anchoredPosition.x + _toastVelocity * Time.deltaTime;

        // Bounce off track walls
        if (x > halfTrack - halfIcon)
        {
            x              =  halfTrack - halfIcon;
            _toastVelocity = -Mathf.Abs(_toastVelocity);
        }
        else if (x < -halfTrack + halfIcon)
        {
            x              = -halfTrack + halfIcon;
            _toastVelocity =  Mathf.Abs(_toastVelocity);
        }

        toastIcon.anchoredPosition = new Vector2(x, toastIcon.anchoredPosition.y);
    }

    // ── Progress ──────────────────────────────────────────────────────────────

    private bool IsToastInsideBar()
    {
        float toastX   = toastIcon.anchoredPosition.x;
        float barLeft  = playerBar.anchoredPosition.x - playerBar.rect.width * 0.5f;
        float barRight = playerBar.anchoredPosition.x + playerBar.rect.width * 0.5f;
        return toastX >= barLeft && toastX <= barRight;
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
