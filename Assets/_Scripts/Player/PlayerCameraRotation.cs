using UnityEngine;

/// <summary>
/// Fixed-Position Incremental Rotation System
/// Attach to the Main Camera. The player character never moves (no WASD).
/// A/D keys rotate the camera left/right by <turnAngle> degrees per press,
/// with smooth SmoothDamp interpolation and optional clamping.
/// </summary>
public class PlayerCameraRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Degrees to rotate per key press.")]
    public float turnAngle = 90f;

    [Tooltip("How fast (in degrees/second) the camera interpolates to its target. Higher = faster.")]
    public float turnSpeed = 5f;

    [Header("Clamping")]
    [Tooltip("When false, rotation is clamped between -180 and +180 degrees from the start orientation. When true, the player can spin freely.")]
    public bool allow360 = false;

    [Tooltip("Maximum angle (absolute) the camera can turn when clamping is active.")]
    public float clampAngle = 180f;

    // Internal state
    private float _targetYaw;       // The yaw we are trying to reach
    private float _currentYaw;      // The actual rendered yaw (smoothed)
    private float _yawVelocity;     // Used by SmoothDamp

    private void Start()
    {
        // Initialise both current and target to the camera's current Y rotation
        _currentYaw = transform.eulerAngles.y;
        _targetYaw  = _currentYaw;
    }

    private void Update()
    {
        HandleInput();
        SmoothRotate();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            _targetYaw += turnAngle;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            _targetYaw -= turnAngle;
        }

        if (!allow360)
        {
            // Clamp target so the camera cannot exceed the limit from its start orientation.
            // We treat 0 as the initial forward direction; wrap _targetYaw to a
            // [-180, 180] range relative to that origin before clamping.
            _targetYaw = Mathf.Clamp(_targetYaw, -clampAngle, clampAngle);
        }
    }

    private void SmoothRotate()
    {
        // SmoothDamp the current yaw towards the target yaw
        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, 1f / turnSpeed);

        // Apply rotation — keep X and Z (pitch/roll) as they were
        Vector3 euler = transform.eulerAngles;
        euler.y = _currentYaw;
        transform.eulerAngles = euler;
    }
}
