using System.Collections;
using UnityEngine;

/// <summary>
/// Generic slicing-station controller. Knows nothing about specific item types —
/// all per-item settings (prefabs, speeds, max slices) come from SliceableItemData.
///
/// Setup:
///  1. Attach to an empty "SlicingController" GameObject.
///  2. Fill in the Scene References in the Inspector.
///  3. Add a SliceableSource component (+ Collider) to every grabbable object in the scene.
///     Assign each one its SliceableItemData asset.
///
/// Flow:
///  - Click any SliceableSource in the scene  → spawns its board item (if board is empty).
///  - Hold LMB on the board item              → knife slices continuously.
///  - After maxSlices cuts the board item is removed; knife returns to start position.
/// </summary>
public class SlicingController : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Where the board item prefab is instantiated.")]
    public Transform boardItemSpawnPoint;

    [Tooltip("The knife 3D object.")]
    public Transform knife;

    [Tooltip("Empty GameObject marking the knife's raised start position.")]
    public Transform highPosition;

    [Tooltip("Empty GameObject marking the cutting-board contact position.")]
    public Transform boardPosition;

    [Tooltip("Empty GameObject at the base of the slice stack (e.g. on a plate).")]
    public Transform slicePlateOrigin;

    [Tooltip("CameraShake component on the Main Camera.")]
    public CameraShake cameraShake;

    [Header("Input")]
    [Tooltip("Maximum raycast distance for click detection.")]
    public float clickRange = 10f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private enum State { Idle, Slicing, Resetting, ReturningToStart }
    private State _state = State.Idle;

    private SliceableItemData _currentItem;   // data for the item currently on the board
    private GameObject        _boardInstance; // the spawned board item
    private Transform         _boardTransform;
    private bool              _cuttingActive;
    private int               _cutCount;
    private float             _activeResetSpeed = 8f; // cached so ReturningToStart works after board is cleared

    // ── Unity loop ────────────────────────────────────────────────────────────

    private void Update()
    {
        HandleClickInput();
        RunStateMachine();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleClickInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject hit = RaycastTarget();
            if (hit == null) return;

            // Did we click a source object?
            SliceableSource source = hit.GetComponent<SliceableSource>()
                                  ?? hit.GetComponentInParent<SliceableSource>();

            if (source != null && source.itemData != null && _boardInstance == null)
            {
                SpawnBoardItem(source.itemData);
                return;
            }

            // Did we click the board item? → enable cutting while LMB is held
            if (_boardInstance != null &&
                (hit == _boardInstance || hit.transform.IsChildOf(_boardInstance.transform)))
            {
                _cuttingActive = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
            _cuttingActive = false;
    }

    // ── State machine ─────────────────────────────────────────────────────────

    private void RunStateMachine()
    {
        // ReturningToStart must tick even after the board item has been cleared
        if (_state == State.ReturningToStart)
        {
            MoveKnifeToward(highPosition.position, _activeResetSpeed);
            if (HasReached(highPosition.position))
            {
                knife.position = highPosition.position;
                _state = State.Idle;
            }
            return;
        }

        if (_boardInstance == null) return;

        switch (_state)
        {
            case State.Idle:
                if (_cuttingActive)
                    _state = State.Slicing;
                break;

            case State.Slicing:
                if (!_cuttingActive) { _state = State.Resetting; break; }

                MoveKnifeToward(boardPosition.position, _currentItem.sliceSpeed);

                if (HasReached(boardPosition.position))
                    OnKnifeHitBoard();
                break;

            case State.Resetting:
                MoveKnifeToward(highPosition.position, _currentItem.resetSpeed);

                if (HasReached(highPosition.position))
                {
                    knife.position = highPosition.position;
                    _state = _cuttingActive ? State.Slicing : State.Idle;
                }
                break;
        }
    }

    // ── Board item lifecycle ──────────────────────────────────────────────────

    private void SpawnBoardItem(SliceableItemData data)
    {
        _currentItem      = data;
        _activeResetSpeed = data.resetSpeed;
        _boardInstance    = Instantiate(data.boardPrefab,
                                        boardItemSpawnPoint.position,
                                        boardItemSpawnPoint.rotation);
        _boardTransform   = _boardInstance.transform;
        _cutCount         = 0;
        _state            = State.Idle;
    }

    private void RemoveBoardItem()
    {
        Destroy(_boardInstance);
        _boardInstance  = null;
        _boardTransform = null;
        _cuttingActive  = false;
        _state          = State.ReturningToStart;
    }

    // ── Core slice event ──────────────────────────────────────────────────────

    private void OnKnifeHitBoard()
    {
        knife.position = boardPosition.position;
        _cutCount++;

        if (_cutCount <= _currentItem.maxSlices)
        {
            if (_currentItem.slicePrefab != null && _boardTransform != null)
            {
                GameObject slice = Instantiate(_currentItem.slicePrefab,
                                               _boardTransform.position,
                                               Quaternion.identity);
                StartCoroutine(FlyToStack(slice.transform, StackPosition(_cutCount - 1)));
            }

            if (cameraShake != null)
                cameraShake.Shake();

            _state = State.Resetting;
        }
        else
        {
            // All slices done — remove the board item and return knife
            RemoveBoardItem();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private GameObject RaycastTarget()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, clickRange)
            ? hit.collider.gameObject
            : null;
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
        if (slicePlateOrigin == null)
            return _boardTransform != null ? _boardTransform.position : Vector3.zero;

        return slicePlateOrigin.position + Vector3.up * index * _currentItem.stackSpacing;
    }

    /// <summary>
    /// Moves the slice to its stack slot while rotating it from (0,0,0) to (0,0,90).
    /// Rotation progress is tied to position progress so both arrive together.
    /// </summary>
    private IEnumerator FlyToStack(Transform slice, Vector3 target)
    {
        Vector3    startPos  = slice.position;
        Quaternion startRot  = Quaternion.identity;
        Quaternion endRot    = Quaternion.Euler(0f, 0f, 90f);
        float      totalDist = Vector3.Distance(startPos, target);

        while (Vector3.Distance(slice.position, target) > 0.005f)
        {
            slice.position = Vector3.MoveTowards(slice.position, target,
                                                 _currentItem.sliceFlySpeed * Time.deltaTime);
            float t = totalDist > 0f
                ? 1f - Vector3.Distance(slice.position, target) / totalDist
                : 1f;
            slice.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        slice.position = target;
        slice.rotation = endRot;
    }
}
