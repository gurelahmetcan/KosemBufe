using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Circular countdown timer displayed in the top-left corner of the screen.
///
/// UI hierarchy (expected):
///   TimerPanel (anchored top-left)
///     └─ CircleFill   — UI Image (type: Filled, method: Radial360, clockwise)
///         └─ TimerText — TextMeshProUGUI showing remaining whole seconds
///
/// The panel starts inactive and is shown/hidden by OrderManager.
/// </summary>
public class OrderTimerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Filled Image representing the circular countdown.")]
    public Image circleFill;

    [Tooltip("TMP text inside the circle showing remaining seconds.")]
    public TextMeshProUGUI timerText;

    [Header("Colors")]
    [Tooltip("Fill color at full time.")]
    public Color normalColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    [Tooltip("Fill color when ≤ 25% time remains.")]
    public Color urgentColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    // ── Runtime ──────────────────────────────────────────────────────────────

    private float _duration;
    private float _remaining;
    private bool  _running;

    /// <summary>True while the timer is actively counting down.</summary>
    public bool IsRunning => _running;

    /// <summary>Seconds left on the clock.</summary>
    public float Remaining => _remaining;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Show the panel and start a countdown of <paramref name="duration"/> seconds.</summary>
    public void StartTimer(float duration)
    {
        _duration  = duration;
        _remaining = duration;
        _running   = true;
        gameObject.SetActive(true);
        Refresh();
    }

    /// <summary>Stop the timer and hide the panel.</summary>
    public void StopTimer()
    {
        _running = false;
        gameObject.SetActive(false);
    }

    // ── Unity loop ───────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_running) return;

        _remaining -= Time.deltaTime;
        if (_remaining < 0f) _remaining = 0f;

        Refresh();
    }

    // ── Visuals ──────────────────────────────────────────────────────────────

    private void Refresh()
    {
        float ratio = _duration > 0f ? _remaining / _duration : 0f;

        if (circleFill != null)
        {
            circleFill.fillAmount = ratio;
            circleFill.color = ratio <= 0.25f ? urgentColor : normalColor;
        }

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(_remaining).ToString();
    }
}
