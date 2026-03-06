using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space or screen-space UI that shows a pulsing "F" icon and a progress bar
/// while the vending machine is in the JAMMED state.
///
/// Setup:
///   1. Create a Canvas (World Space recommended, parented to the machine).
///   2. Add a pulsing icon (e.g. the letter "F") as promptIcon.
///   3. Add a Slider for the progress bar (progressBar).
///   4. Assign this component on the same GameObject or the machine.
///   5. The VendingMachine drives Show/Hide/SetProgress.
/// </summary>
public class MashMeterUI : MonoBehaviour
{
    [Tooltip("Root GameObject of the mash prompt UI (icon + bar). Toggled on/off.")]
    public GameObject rootPanel;

    [Tooltip("The 'F' icon RectTransform that pulses to grab attention.")]
    public RectTransform promptIcon;

    [Tooltip("Slider (0–1) showing mash progress.")]
    public Slider progressBar;

    [Header("Pulse Animation")]
    [Tooltip("Scale multiplier at the peak of the pulse.")]
    public float pulseScale = 1.3f;

    [Tooltip("Pulses per second.")]
    public float pulseSpeed = 3f;

    [Header("Fill Colors")]
    [Tooltip("Progress bar fill Image — color lerps from start to end as it fills.")]
    public Image fillImage;
    public Color fillColorStart = Color.red;
    public Color fillColorEnd   = Color.green;

    // ── Runtime ─────────────────────────────────────────────────────────────────

    private Vector3 _iconBaseScale;
    private bool    _isVisible;

    private void Awake()
    {
        if (promptIcon != null)
            _iconBaseScale = promptIcon.localScale;

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void Update()
    {
        if (!_isVisible) return;
        PulseIcon();
    }

    // ── Public API ──────────────────────────────────────────────────────────────

    public void Show()
    {
        _isVisible = true;
        if (rootPanel != null)
            rootPanel.SetActive(true);
        SetProgress(0f);
    }

    public void Hide()
    {
        _isVisible = false;
        if (rootPanel != null)
            rootPanel.SetActive(false);

        // Reset icon scale
        if (promptIcon != null)
            promptIcon.localScale = _iconBaseScale;
    }

    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);

        if (progressBar != null)
            progressBar.value = t;

        if (fillImage != null)
            fillImage.color = Color.Lerp(fillColorStart, fillColorEnd, t);
    }

    // ── Pulse ───────────────────────────────────────────────────────────────────

    private void PulseIcon()
    {
        if (promptIcon == null) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(1f, pulseScale, t);
        promptIcon.localScale = _iconBaseScale * scale;
    }
}
