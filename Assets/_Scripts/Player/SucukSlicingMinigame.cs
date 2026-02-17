using UnityEngine;

/// <summary>
/// Hold-to-Slice minigame for the Sucuk Dilimleme (Sausage Slicing) station.
///
/// Setup:
///  1. Create an empty GameObject in the scene named "SlicingController" and attach this script.
///  2. Assign the Knife, Sucuk, HighPosition, BoardPosition transforms in the Inspector.
///  3. Assign a small prefab (e.g. a thin cylinder) as SlicePrefab.
///  4. Attach CameraShake to the Main Camera and drag it into the CameraShake field.
/// </summary>
public class SucukSlicingMinigame : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The knife 3D object.")]
    public Transform knife;

    [Tooltip("The sucuk (sausage) 3D object.")]
    public Transform sucuk;

    [Tooltip("Empty GameObject marking the knife's raised start position.")]
    public Transform highPosition;

    [Tooltip("Empty GameObject marking the cutting-board contact position.")]
    public Transform boardPosition;

    [Tooltip("Prefab instantiated for each slice (e.g. a thin disc/cylinder).")]
    public GameObject slicePrefab;

    [Tooltip("CameraShake component on the Main Camera.")]
    public CameraShake cameraShake;

    [Header("Slice Settings")]
    [Tooltip("Speed (units/sec) at which the knife descends.")]
    public float sliceSpeed = 3f;

    [Tooltip("How far (units) the sucuk advances after each slice.")]
    public float sliceWidth = 0.1f;

    [Tooltip("Speed (units/sec) at which the knife resets to the high position.")]
    public float resetSpeed = 8f;

    // ── State machine ────────────────────────────────────────────────────────
    private enum State { Idle, Slicing, Resetting }
    private State _state = State.Idle;

    private void Update()
    {
        switch (_state)
        {
            case State.Idle:
                if (Input.GetMouseButton(0))
                    _state = State.Slicing;
                break;

            case State.Slicing:
                // Release mid-slice → start resetting
                if (!Input.GetMouseButton(0))
                {
                    _state = State.Resetting;
                    break;
                }

                MoveKnifeToward(boardPosition.position, sliceSpeed);

                if (HasReached(boardPosition.position))
                    OnKnifeHitBoard();
                break;

            case State.Resetting:
                MoveKnifeToward(highPosition.position, resetSpeed);

                if (HasReached(highPosition.position))
                {
                    knife.position = highPosition.position;
                    // If LMB is still held, immediately start the next slice
                    _state = Input.GetMouseButton(0) ? State.Slicing : State.Idle;
                }
                break;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void MoveKnifeToward(Vector3 target, float speed)
    {
        knife.position = Vector3.MoveTowards(knife.position, target, speed * Time.deltaTime);
    }

    private bool HasReached(Vector3 target)
    {
        return Vector3.Distance(knife.position, target) < 0.005f;
    }

    private void OnKnifeHitBoard()
    {
        // Snap precisely to board so slices don't drift
        knife.position = boardPosition.position;

        // Spawn a slice at the current knife/board position
        if (slicePrefab != null)
            Instantiate(slicePrefab, knife.position, sucuk != null ? sucuk.rotation : Quaternion.identity);

        // Advance the sucuk log forward by one slice thickness
        if (sucuk != null)
            sucuk.position += sucuk.forward * sliceWidth;

        // Tactile screen shake
        if (cameraShake != null)
            cameraShake.Shake();

        // Begin reset phase
        _state = State.Resetting;
    }
}
