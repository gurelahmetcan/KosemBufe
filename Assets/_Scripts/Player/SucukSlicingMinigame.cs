using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Hold-to-Slice minigame for the Sucuk Dilimleme (Sausage Slicing) station.
///
/// Setup:
///  1. Create an empty GameObject named "SlicingController" and attach this script.
///  2. Assign all Inspector references (see fields below).
///  3. Attach CameraShake to the Main Camera and drag it into the CameraShake field.
///  4. Ensure the HangingSucuk and any spawned BoardSucuk have Colliders so raycasts land.
///
/// Flow:
///  - Click the hanging sucuk     → spawns a board sucuk (if none is present).
///  - Hold LMB on the board sucuk → knife descends and slices continuously.
///  - After 5 slices the 6th cut removes the board sucuk; player can grab a new one.
/// </summary>
public class SucukSlicingMinigame : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The sucuk object hanging in the scene that the player clicks to get a new one.")]
    public GameObject hangingSucuk;

    [Tooltip("Prefab spawned onto the cutting board when the hanging sucuk is clicked.")]
    public GameObject boardSucukPrefab;

    [Tooltip("Where the board sucuk prefab is instantiated (position + rotation).")]
    public Transform boardSucukSpawnPoint;

    [Tooltip("The knife 3D object.")]
    public Transform knife;

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

    [Tooltip("Speed (units/sec) at which the knife resets to the high position.")]
    public float resetSpeed = 8f;

    [Tooltip("Max raycast distance for click detection.")]
    public float clickRange = 10f;

    [Header("Slice Animation")]
    [Tooltip("Speed (units/sec) at which each spawned slice flies to the plate.")]
    public float sliceFlySpeed = 6f;

    [Tooltip("Y distance between each stacked slice on the plate.")]
    public float stackSpacing = 0.1f;

    // ── Runtime state ────────────────────────────────────────────────────────
    private enum State { Idle, Slicing, Resetting }
    private State _state = State.Idle;

    private GameObject _boardSucukInstance;  // null when no sucuk is on the board
    private Transform  _sucuk;               // shortcut to _boardSucukInstance.transform
    private bool       _cuttingActive;       // true while LMB is held after clicking board sucuk
    private int        _cutCount;
    private Vector3 _initialKnifePos;

    private const int MaxSlices = 5;         // cuts 1-5 produce slices
                                             // cut 6 removes the board sucuk
                                             
    private void Start()
    {
        if (knife != null)
        {
            _initialKnifePos = knife.position;
            Debug.Log($"Initial Knife Pos: {_initialKnifePos}");
        }
    }

    private void Update()
    {
        HandleClickInput();
        RunStateMachine();
    }

    // ── Input ────────────────────────────────────────────────────────────────

    private void HandleClickInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject hit = RaycastTarget();

            // Click on hanging sucuk → spawn board sucuk if none exists
            if (hit == hangingSucuk && _boardSucukInstance == null)
            {
                SpawnBoardSucuk();
                return;
            }

            // Click on board sucuk → begin cutting
            if (_boardSucukInstance != null && hit != null &&
                (hit == _boardSucukInstance || hit.transform.IsChildOf(_boardSucukInstance.transform)))
            {
                _cuttingActive = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
            _cuttingActive = false;
    }

    private void RunStateMachine()
    {
        if (_boardSucukInstance == null) return;

        switch (_state)
        {
            case State.Idle:
                if (_cuttingActive)
                    _state = State.Slicing;
                break;

            case State.Slicing:
                if (!_cuttingActive)
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
                    _state = _cuttingActive ? State.Slicing : State.Idle;
                }
                break;
        }
    }

    // ── Board sucuk lifecycle ─────────────────────────────────────────────────

    private void SpawnBoardSucuk()
    {
        _boardSucukInstance = Instantiate(boardSucukPrefab,
                                          boardSucukSpawnPoint.position,
                                          boardSucukSpawnPoint.rotation);
        _sucuk    = _boardSucukInstance.transform;
        _cutCount = 0;
        _state    = State.Idle;
    }

    private void RemoveBoardSucuk()
    {
        if (_boardSucukInstance != null)
            _boardSucukInstance.SetActive(false);

        Destroy(_boardSucukInstance.gameObject);
        _boardSucukInstance = null;
        _sucuk              = null;
        _cuttingActive      = false;
        _state              = State.Idle;
        MoveKnifeToward(_initialKnifePos, resetSpeed);
    }

    // ── Core slice event ──────────────────────────────────────────────────────

    private void OnKnifeHitBoard()
    {
        knife.position = boardPosition.position;
        _cutCount++;

        if (_cutCount <= MaxSlices)
        {
            // Spawn slice at sucuk world position; rotate (0,0,0)→(0,0,90) mid-flight
            if (slicePrefab != null && _sucuk != null)
            {
                GameObject slice = Instantiate(slicePrefab, _sucuk.position, Quaternion.identity);
                Vector3 stackTarget = StackPosition(_cutCount - 1);
                StartCoroutine(FlyToStack(slice.transform, stackTarget));
            }

            if (cameraShake != null)
                cameraShake.Shake();

            _state = State.Resetting;
        }
        else
        {
            // 6th cut — clear the board so the player can grab a new sucuk
            RemoveBoardSucuk();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Casts a ray from the centre of the screen and returns the hit GameObject.</summary>
    private GameObject RaycastTarget()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, clickRange) ? hit.collider.gameObject : null;
    }

    private void MoveKnifeToward(Vector3 target, float speed)
    {
        knife.position = Vector3.MoveTowards(knife.position, target, speed * Time.deltaTime);
    }

    private bool HasReached(Vector3 target)
    {
        return Vector3.Distance(knife.position, target) < 0.005f;
    }

    private Vector3 StackPosition(int index)
    {
        if (slicePlateOrigin == null) return _sucuk != null ? _sucuk.position : Vector3.zero;
        return slicePlateOrigin.position + Vector3.up * index * stackSpacing;
    }

    private IEnumerator FlyToStack(Transform slice, Vector3 target)
    {
        Vector3    startPos  = slice.position;
        Quaternion startRot  = Quaternion.identity;
        Quaternion endRot    = Quaternion.Euler(0f, 0f, 90f);
        float      totalDist = Vector3.Distance(startPos, target);

        while (Vector3.Distance(slice.position, target) > 0.005f)
        {
            slice.position = Vector3.MoveTowards(slice.position, target, sliceFlySpeed * Time.deltaTime);
            float t = totalDist > 0f ? 1f - Vector3.Distance(slice.position, target) / totalDist : 1f;
            slice.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        slice.position = target;
        slice.rotation = endRot;
    }
}
