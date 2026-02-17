using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the Main Camera.
/// Call Shake() from any script to trigger a brief positional shake.
/// The camera returns to its exact original local position when done.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Maximum displacement (units) during a shake.")]
    public float magnitude = 0.1f;

    [Tooltip("Duration of the shake in seconds.")]
    public float duration = 0.15f;

    private Vector3 _originalLocalPosition;
    private Coroutine _activeShake;

    private void Awake()
    {
        _originalLocalPosition = transform.localPosition;
    }

    /// <summary>Trigger a screen shake. Safe to call while a shake is already active.</summary>
    public void Shake()
    {
        // Cancel any in-progress shake so we don't accumulate offset
        if (_activeShake != null)
            StopCoroutine(_activeShake);

        _activeShake = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Random offset in all three axes; feel free to restrict to XY only
            Vector3 offset = Random.insideUnitSphere * magnitude;
            transform.localPosition = _originalLocalPosition + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Always restore the exact rest position
        transform.localPosition = _originalLocalPosition;
        _activeShake = null;
    }
}
