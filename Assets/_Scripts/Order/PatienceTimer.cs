using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space patience countdown displayed above a customer's head.
/// Starts automatically when StartTimer is called and can be paused/resumed.
/// Fires <see cref="OnExpired"/> when time runs out.
///
/// Scene Setup (child of Customer prefab):
///   Canvas (World Space, small scale ~0.01)
///     └─ PatiencePanel
///         └─ CircleFill  — UI Image (Filled, Radial360, clockwise)
///             └─ TimerText — TextMeshProUGUI (optional, shows seconds)
/// </summary>
public class PatienceTimer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Filled Image representing the circular countdown.")]
    public Image circleFill;

    [Tooltip("Optional TMP text showing remaining seconds.")]
    public TextMeshProUGUI timerText;

    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.7f, 0.9f, 1f);
    public Color urgentColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    /// <summary>Fired once when the timer reaches zero.</summary>
    public event Action OnExpired;

    /// <summary>True while the timer is actively counting down (not paused).</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Seconds remaining on the patience clock.</summary>
    public float Remaining => _remaining;

    private float _duration;
    private float _remaining;
    private bool _expired;

    /// <summary>Begin the countdown with the given duration in seconds.</summary>
    public void StartTimer(float duration)
    {
        _duration  = duration;
        _remaining = duration;
        _expired   = false;
        IsRunning  = true;
        Refresh();
    }

    /// <summary>Pause the countdown (keeps current remaining time).</summary>
    public void Pause()
    {
        IsRunning = false;
    }

    /// <summary>Resume the countdown from where it was paused.</summary>
    public void Resume()
    {
        if (!_expired)
            IsRunning = true;
    }

    /// <summary>Stop the timer completely and hide visuals.</summary>
    public void Stop()
    {
        IsRunning = false;
        _expired  = true;

        if (circleFill != null)
            circleFill.gameObject.SetActive(false);
        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsRunning) return;

        _remaining -= Time.deltaTime;

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            IsRunning  = false;
            _expired   = true;
            Refresh();
            OnExpired?.Invoke();
            return;
        }

        Refresh();
    }

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
