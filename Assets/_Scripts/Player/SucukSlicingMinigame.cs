using System.Collections;
using UnityEngine;

/// <summary>
/// Hold-to-Slice minigame for the Sucuk Dilimleme (Sausage Slicing) station.
///
/// Setup:
///  1. Create an empty GameObject named "SlicingController" and attach this script.
///  2. Assign Knife, Sucuk, HighPosition, BoardPosition transforms in the Inspector.
///  3. Assign a slice prefab (thin disc/cylinder) as SlicePrefab.
///  4. Create an empty GameObject where slices should stack and assign it to SlicePlateOrigin.
///  5. Attach CameraShake to the Main Camera and drag it into the CameraShake field.
///
/// Behaviour:
///  - Cuts 1–5 each spawn a slice and shrink the sucuk along its X axis by sliceWidth.
///  - Cut 6 removes the sucuk object entirely (no slice spawned).
///  - Each spawned slice appears at the sucuk's world position then flies to the plate stack.
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

    [Tooltip("Empty GameObject at the base of the slice stack (e.g. on a plate).")]
    public Transform slicePlateOrigin;

    [Tooltip("Prefab instantiated for each slice (e.g. a thin disc/cylinder).")]
    public GameObject slicePrefab;

    [Tooltip("CameraShake component on the Main Camera.")]
    public CameraShake cameraShake;

    [Header("Slice Settings")]
    [Tooltip("Speed (units/sec) at which the knife descends.")]
    public float sliceSpeed = 3f;

    [Tooltip("How much the sucuk's X scale shrinks per slice (also the slice's X scale).")]
    public float sliceWidth = 0.1f;

    [Tooltip("Speed (units/sec) at which the knife resets to the high position.")]
    public float resetSpeed = 8f;

    [Header("Slice Animation")]
    [Tooltip("Speed (units/sec) at which each spawned slice flies to the plate.")]
    public float sliceFlySpeed = 6f;

    [Tooltip("Extra vertical spacing between stacked slices on the plate.")]
    public float stackSpacing = 0.05f;

    // ── State machine ────────────────────────────────────────────────────────
    private enum State { Idle, Slicing, Resetting, Finished }
    private State _state = State.Idle;

    // Total cut count (slices spawned = _cutCount for cuts 1-5, cut 6 removes sucuk)
    private int _cutCount = 0;
    private const int MaxCuts = 6;   // 5 slices + 1 final removal
    private const int MaxSlices = 5; // MaxCuts - 1

    private void Update()
    {
        if (_state == State.Finished) return;

        switch (_state)
        {
            case State.Idle:
                if (Input.GetMouseButton(0))
                    _state = State.Slicing;
                break;

            case State.Slicing:
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
                    _state = Input.GetMouseButton(0) ? State.Slicing : State.Idle;
                }
                break;
        }
    }

    // ── Core event ───────────────────────────────────────────────────────────

    private void OnKnifeHitBoard()
    {
        knife.position = boardPosition.position;
        _cutCount++;

        if (_cutCount <= MaxSlices)
        {
            // Shrink the sucuk's X scale instead of moving it
            if (sucuk != null)
            {
                Vector3 scale = sucuk.localScale;
                scale.x -= sliceWidth;
                sucuk.localScale = scale;
            }

            // Spawn slice at the sucuk's current world position, then fly to plate
            if (slicePrefab != null && sucuk != null)
            {
                GameObject slice = Instantiate(slicePrefab, sucuk.position, sucuk.rotation);
                Vector3 stackTarget = StackPosition(_cutCount - 1);
                StartCoroutine(FlyToStack(slice.transform, stackTarget));
            }

            // Screen shake for tactile feedback
            if (cameraShake != null)
                cameraShake.Shake();

            _state = State.Resetting;
        }
        else
        {
            // 6th cut — remove the remaining sucuk stub and end the minigame
            if (sucuk != null)
                sucuk.gameObject.SetActive(false);

            _state = State.Finished;
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

    /// <summary>Returns the world position for the nth slice in the plate stack.</summary>
    private Vector3 StackPosition(int index)
    {
        if (slicePlateOrigin == null) return sucuk != null ? sucuk.position : Vector3.zero;
        return slicePlateOrigin.position + Vector3.up * index * (sliceWidth + stackSpacing);
    }

    private IEnumerator FlyToStack(Transform slice, Vector3 target)
    {
        while (Vector3.Distance(slice.position, target) > 0.005f)
        {
            slice.position = Vector3.MoveTowards(slice.position, target, sliceFlySpeed * Time.deltaTime);
            yield return null;
        }
        slice.position = target;
    }
}
