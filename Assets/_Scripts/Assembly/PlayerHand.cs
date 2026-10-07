using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using TMPro;

namespace Bufe
{
    public class PlayerHand : MonoBehaviour
    {
        [Header("Hold Point")]
        [Tooltip("Camera child where held items sit.")]
        public Transform holdPoint;

        [Header("Input")]
        [Tooltip("Max click reach.")]
        public float interactRange = 10f;

        [Tooltip("Throw force.")]
        public float throwForce = 5f;

        [Tooltip("Right-click hold time that counts as a throw.")]
        public float throwHoldThreshold = 0.25f;

        [Header("Drop")]
        [Tooltip("Max time a dropped item ignores player collision.")]
        public float releaseIgnoreTimeout = 2f;

        [Header("UI")]
        [Tooltip("Optional text showing the held item.")]
        public TextMeshProUGUI handStatusText;

        [Header("Arms")]
        [Tooltip("Arms animator. Drives int 'Grip': 0 holding, -1 empty.")]
        [SerializeField] private Animator armsAnimator;

        [Tooltip("Reach-and-grab on pickup. Auto-found. None = instant.")]
        [SerializeField] private HandReach handReach;

        /// <summary>Layer the Viewmodelcamera renders - the arms and whatever they hold.</summary>
        private const string ViewmodelLayer = "Viewmodel";

        private static readonly int GripParam = Animator.StringToHash("Grip");
        private const int GripHolding = 0;
        private const int GripEmpty   = -1;

        // ── Runtime state ────────────────────────────────────────────────────────

        private InteractableItem _heldItem;
        private Rigidbody        _heldRb;
        private Collider[]       _heldColliders;
        private Transform[]      _heldLayerTransforms;
        private int[]            _heldOriginalLayers;
        private string           _hoverLabel;
        private float            _rightPressTime = -1f;
        private bool             _suppressRightRelease;

        // ── Hover cache ──────────────────────────────────────────────────────────
        // The crosshair ray and the component lookups behind it used to run two or
        // three times a frame (once for the click, once for the hover label). They
        // now run once: the ray is cached per frame, and the components resolved from
        // whatever it hit are cached until the player looks at something else.

        private GameObject _frameHit;              // collider hit by this frame's ray
        private Vector3    _frameHitPoint;         // where that ray hit it (world space)
        private int        _frameHitStamp = -1;    // frame the cached ray was cast on

        private GameObject             _resolvedFor;   // object the lookups below describe
        private IAssemblyInteractable  _rInteractable;
        private InteractableItem       _rItem;
        private ISecondaryInteractable _rSecondary;
        private IHoverLabel            _rHoverLabel;
        private string                 _rName;         // cached: GameObject.name allocates on every read

        private GameObject _outlinedRoot;          // currently outlined object, null when none

        // The player's own colliders - the CharacterController capsule among them.
        // A dropped item ignores these until it has fallen clear of them.
        private Collider[] _selfColliders;

        // Whether armsAnimator's controller actually has an int "Grip" parameter.
        // Resolved once per controller, so a missing parameter stays silent instead
        // of logging an Animator warning every frame.
        private RuntimeAnimatorController _gripCheckedFor;
        private bool                      _hasGripParam;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Fired right after an interactable was clicked, with the object that ran
        /// the interaction. The tutorial listens to this to know "they opened the
        /// shutter", "they clicked the toaster" without every station having to
        /// know a tutorial exists.
        /// </summary>
        public event Action<GameObject> OnInteracted;

        /// <summary>
        /// The object a left-click would act on right now - the one wearing the hover
        /// outline - or null when the crosshair is on nothing usable.
        /// </summary>
        public GameObject OutlinedRoot => _outlinedRoot;

        public InteractableItem HeldItem    => _heldItem;
        public bool             IsHandEmpty => _heldItem == null;

        /// <summary>Set by stations (e.g. during appliance processing) to block all input.</summary>
        public bool InputBlocked { get; set; }

        // ── Read by InteractionHintUI ────────────────────────────────────────────
        // All three describe the state resolved by this frame's UpdateHover, so read
        // them from LateUpdate (or a later execution order) to avoid a frame of lag.

        /// <summary>Name of whatever the crosshair is on, or null when there is nothing to name.</summary>
        public string HoverLabel => _hoverLabel;

        /// <summary>True when a left-click would do something right now.</summary>
        public bool HasAction => _outlinedRoot != null;

        /// <summary>True when an empty-hand right-click would do something right now.</summary>
        public bool HasSecondaryAction =>
            _heldItem == null && _rSecondary != null && _rSecondary.CanSecondaryInteract(this);

        // ── Unity loop ───────────────────────────────────────────────────────────

        private void Awake()
        {
            _selfColliders = transform.root.GetComponentsInChildren<Collider>(true);

            if (handReach == null)
                handReach = transform.root.GetComponentInChildren<HandReach>(true);

            // Not set up in the scene: add it to the rigged arms (the object with the
            // Rig Builder) so the reach works without an extra setup step.
            if (handReach == null)
            {
                RigBuilder rig = transform.root.GetComponentInChildren<RigBuilder>(true);
                if (rig != null)
                {
                    handReach = rig.gameObject.AddComponent<HandReach>();
                    Log.Info($"[PlayerHand] No HandReach found - added one to '{rig.name}'.", rig);
                }
            }
        }

        private void Update()
        {
            // Before the InputBlocked early-out: the arms must follow the hand even
            // while a station is driving the player.
            UpdateArmsGrip();

            if (InputBlocked)
            {
                // A station is driving the player (e.g. an appliance is processing).
                // Drop the outline too - it would otherwise sit there inviting a
                // click that cannot happen.
                ClearHover();
                return;
            }

            HandleInput();
            UpdateHover();
        }

        private void OnDisable() => ClearHover();

        // ── Input ────────────────────────────────────────────────────────────────

        private void HandleInput()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_heldItem == null)
                {
                    // Empty hand: right-click is a "take back" / secondary interaction
                    // (e.g. lifting the bread off the prep station). Suppress the
                    // matching release so it doesn't immediately drop what we grabbed.
                    _suppressRightRelease = TrySecondaryInteract();
                }
                else
                {
                    _rightPressTime = Time.time;
                }
            }
            else if (mouse.rightButton.wasReleasedThisFrame)
            {
                if (_suppressRightRelease)
                {
                    _suppressRightRelease = false;
                }
                else if (_heldItem != null)
                {
                    bool throwIt = _rightPressTime >= 0f && Time.time - _rightPressTime >= throwHoldThreshold;
                    Drop(throwIt);
                }
                _rightPressTime = -1f;
            }

            if (mouse.rightButton.isPressed) return;

            if (!mouse.leftButton.wasPressedThisFrame) return;

            // A new click abandons a reach still in flight - its pick-up is dropped
            // and this click is handled normally (it may start a new reach).
            if (handReach != null && handReach.IsReaching)
                handReach.Cancel();

            if (!ResolveHover()) return;

            if (_rInteractable != null && _rInteractable.CanInteract(this))
            {
                // Captured before the call: an interaction can destroy or replace
                // the object it ran on (a storage handing out a fresh item).
                var interacted = ((Component)_rInteractable).gameObject;
                _rInteractable.Interact(this);
                OnInteracted?.Invoke(interacted);
                return;
            }

            if (_heldItem == null && _rItem != null)
                PickUpWithReach(_rItem);
        }

        /// <summary>
        /// Clicked a loose item: let the hand reach for it and run the normal PickUp when
        /// it arrives. Falls back to an instant PickUp when there is no HandReach or it
        /// can't run, so the mechanic never depends on the animation.
        /// </summary>
        private void PickUpWithReach(InteractableItem item)
        {
            // By the time the hand arrives the item may be gone, or another pick-up
            // (e.g. from a storage) may have filled the hand - PickUp ignores the latter.
            if (handReach != null && handReach.TryReach(item, () => { if (item != null) PickUp(item); }))
                return;

            PickUp(item);
        }

        /// <summary>
        /// For interactables that put something into the hand (storages): the hand first
        /// reaches to where the player clicked, then <paramref name="onArrive"/> runs -
        /// that is where the item should be spawned and picked up. Runs it immediately
        /// when there is no HandReach or it can't run. A new click before arrival
        /// cancels it, so onArrive must re-check its own preconditions.
        /// </summary>
        public void ReachThen(Action onArrive)
        {
            if (onArrive == null) return;

            // The click that got us here resolved the crosshair this frame, so the
            // cached hit point is the spot on the interactable that was clicked.
            if (handReach != null && _frameHit != null && handReach.TryReach(_frameHitPoint, onArrive))
                return;

            onArrive();
        }

        /// <summary>
        /// Empty-hand right-click. Fires the secondary interaction on whatever is
        /// under the crosshair (e.g. lift the bread back off the prep station).
        /// Returns true if something handled it.
        /// </summary>
        private bool TrySecondaryInteract()
        {
            if (!ResolveHover()) return false;

            if (_rSecondary != null && _rSecondary.CanSecondaryInteract(this))
            {
                _rSecondary.SecondaryInteract(this);
                return true;
            }

            return false;
        }

        // ── Hover detection ──────────────────────────────────────────────────────

        /// <summary>
        /// Casts the crosshair ray (at most once per frame) and refreshes the cached
        /// component lookups for whatever it hit. Returns false when the crosshair is
        /// over nothing within reach.
        /// </summary>
        private bool ResolveHover()
        {
            GameObject hit = RaycastTarget();

            if (hit != _resolvedFor)
            {
                _resolvedFor   = hit;
                _rName         = hit != null ? hit.name : null;
                _rInteractable = hit != null ? hit.GetComponentInParent<IAssemblyInteractable>()  : null;
                _rItem         = hit != null ? hit.GetComponentInParent<InteractableItem>()       : null;
                _rSecondary    = hit != null ? hit.GetComponentInParent<ISecondaryInteractable>() : null;
                _rHoverLabel   = hit != null ? hit.GetComponentInParent<IHoverLabel>()            : null;
            }

            return hit != null;
        }

        private void UpdateHover()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                ClearHover();
                return;
            }

            bool hasTarget = ResolveHover();

            SetOutline(hasTarget ? ActionTarget() : null);

            // Status text label
            string label = hasTarget ? GetHoverLabel() : null;
            if (label == _hoverLabel) return;
            _hoverLabel = label;
            RefreshUI();
        }

        private void ClearHover()
        {
            SetOutline(null);

            if (_hoverLabel == null) return;
            _hoverLabel = null;
            RefreshUI();
        }

        /// <summary>
        /// The object a left-click would act on given the current hand state, or null
        /// when nothing under the crosshair is usable right now - a fridge door while
        /// the hand is full, an empty storage, a station that is already occupied.
        ///
        /// This mirrors the precedence in <see cref="HandleInput"/> on purpose: the
        /// outline is driven by it, so it can never promise an interaction that the
        /// click does not deliver.
        /// </summary>
        private GameObject ActionTarget()
        {
            if (_rInteractable != null && _rInteractable.CanInteract(this))
                return ((Component)_rInteractable).gameObject;

            if (_heldItem == null)
            {
                if (_rItem != null)
                    return _rItem.gameObject;

                if (_rSecondary != null && _rSecondary.CanSecondaryInteract(this))
                    return ((Component)_rSecondary).gameObject;
            }

            return null;
        }

        /// <summary>Hands the outline system the object the player can act on, if it changed.</summary>
        private void SetOutline(GameObject root)
        {
            if (root == _outlinedRoot) return;
            _outlinedRoot = root;
            HoverOutlineService.SetTarget(root);
        }

        private string GetHoverLabel()
        {
            // Custom labels first (e.g. storages showing "Name - N left"). A component
            // that implements the interface but leaves the label empty falls through to
            // the generic names below rather than leaving the player with nothing.
            if (_rHoverLabel != null)
            {
                string custom = _rHoverLabel.HoverLabel;
                if (!string.IsNullOrEmpty(custom)) return custom;
            }

            if (_rItem != null) return _rItem.itemName;

            if (_rInteractable != null) return _rName;

            return null;
        }

        // ── Hand operations ──────────────────────────────────────────────────────

        public void PickUp(InteractableItem item)
        {
            if (_heldItem != null) return;

            _heldItem = item;

            _heldColliders = item.GetComponentsInChildren<Collider>();
            foreach (Collider col in _heldColliders)
                col.enabled = false;

            _heldRb = item.GetComponent<Rigidbody>();
            if (_heldRb != null)
            {
                if (!_heldRb.isKinematic)
                {
                    _heldRb.linearVelocity  = Vector3.zero;
                    _heldRb.angularVelocity = Vector3.zero;
                }
                _heldRb.isKinematic = true;
            }

            // The hold point is a bone deep inside the arm rig, so it carries whatever
            // scale that rig was built at. PlaceInHand poses the item in metres
            // against that scale, so the rig can be rescaled without resizing or
            // displacing everything the player picks up.
            item.PlaceInHand(holdPoint);

            ApplyViewmodelLayer(item);

            item.OnPickedUp();
            InvalidateHover();
            RefreshUI();
        }

        public void Drop(bool throwIt = true)
        {
            if (_heldItem == null) return;

            InteractableItem item = _heldItem;
            _heldItem = null;

            item.transform.SetParent(null);
            item.RestoreWorldScale();
            RestoreWorldLayer();

            Collider[] itemColliders = _heldColliders;
            if (itemColliders != null)
                foreach (Collider col in itemColliders)
                    if (col != null) col.enabled = true;

            // The item leaves the hand inside the player's own capsule, so let it
            // fall through the player instead of being shoved out of it.
            IgnorePlayerCollisionsUntilClear(item, itemColliders);

            Camera cam = Camera.main;
            if (_heldRb != null)
            {
                _heldRb.isKinematic     = false;
                _heldRb.angularVelocity = Vector3.zero;
                _heldRb.linearVelocity  = (throwIt && cam != null) ? cam.transform.forward * throwForce : Vector3.zero;
            }
            
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.ItemFall);

            item.OnDropped();

            _heldRb        = null;
            _heldColliders = null;
            InvalidateHover();
            RefreshUI();
        }

        /// <summary>
        /// Releases the held item without repositioning or applying physics.
        /// The caller is responsible for positioning and managing the item.
        /// Returns the released item.
        /// </summary>
        public InteractableItem Release()
        {
            if (_heldItem == null) return null;

            InteractableItem item = _heldItem;
            _heldItem = null;

            item.transform.SetParent(null);
            item.RestoreWorldScale();
            RestoreWorldLayer();

            _heldRb        = null;
            _heldColliders = null;
            InvalidateHover();
            RefreshUI();

            return item;
        }

        /// <summary>Destroys the held item. Used when delivering an order.</summary>
        public void DestroyHeldItem()
        {
            if (_heldItem == null) return;

            Destroy(_heldItem.gameObject);
            _heldItem            = null;
            _heldRb              = null;
            _heldColliders       = null;
            _heldLayerTransforms = null;
            _heldOriginalLayers  = null;
            InvalidateHover();
            RefreshUI();
        }

        // ── Releasing an item into the world ──────────────────────────────────────
        // The hold point sits on the camera, which is inside the player's own
        // CharacterController capsule. An item handed back to physics there is handed
        // over already overlapping the player, and the first physics step resolves
        // that overlap the only way it can: by shoving the item out. That shove is
        // the "flung forwards and left hanging in mid-air" drop - the throw force has
        // nothing to do with it, a plain drop does it too.
        //
        // The item is left exactly where the hand had it and falls under gravity as
        // it should; it just stops colliding with the player until it is clear, so
        // there is no overlap for physics to resolve. Collisions come back as soon as
        // the item has fallen out of the player, so it cannot be kicked around or
        // stood on through the capsule afterwards.

        private void IgnorePlayerCollisionsUntilClear(InteractableItem item, Collider[] itemColliders)
        {
            if (item == null || itemColliders == null || _selfColliders == null) return;

            StartCoroutine(ReleaseCollisionRoutine(item, itemColliders));
        }

        private IEnumerator ReleaseCollisionRoutine(InteractableItem item, Collider[] itemColliders)
        {
            // Must run after the item's colliders are back on: Unity drops an ignore
            // pair when either collider is disabled, so setting it while they were
            // still off would be forgotten immediately.
            SetPlayerCollisionIgnored(itemColliders, true);

            float deadline = Time.time + releaseIgnoreTimeout;

            while (item != null && Time.time < deadline && OverlapsPlayer(itemColliders))
                yield return new WaitForFixedUpdate();

            if (item != null)
                SetPlayerCollisionIgnored(itemColliders, false);
        }

        private void SetPlayerCollisionIgnored(Collider[] itemColliders, bool ignored)
        {
            foreach (Collider itemCol in itemColliders)
            {
                if (itemCol == null || !itemCol.enabled) continue;

                foreach (Collider selfCol in _selfColliders)
                {
                    if (selfCol == null || !selfCol.enabled) continue;
                    Physics.IgnoreCollision(itemCol, selfCol, ignored);
                }
            }
        }

        /// <summary>
        /// Bounds test rather than a real overlap query: it only decides when to give
        /// the collision back, and erring on the side of "still overlapping" for a
        /// frame or two costs nothing.
        /// </summary>
        private bool OverlapsPlayer(Collider[] itemColliders)
        {
            foreach (Collider itemCol in itemColliders)
            {
                if (itemCol == null || !itemCol.enabled) continue;

                foreach (Collider selfCol in _selfColliders)
                {
                    if (selfCol == null || !selfCol.enabled) continue;
                    if (itemCol.bounds.Intersects(selfCol.bounds)) return true;
                }
            }

            return false;
        }

        // ── Viewmodel layer ───────────────────────────────────────────────────────
        // The arms are drawn by Viewmodelcamera: an overlay camera with its own,
        // narrower FOV that clears depth and then paints over everything the base
        // camera rendered. An item left on its world layer is drawn by the base
        // camera instead, so it sorts behind the hand however close it actually sits,
        // and it is projected through the wrong FOV, so it does not even line up with
        // the grip. Handing the item to the same camera as the arms fixes both.

        private void ApplyViewmodelLayer(InteractableItem item)
        {
            int layer = LayerMask.NameToLayer(ViewmodelLayer);
            if (layer < 0) return;

            _heldLayerTransforms = item.GetComponentsInChildren<Transform>(true);
            _heldOriginalLayers  = new int[_heldLayerTransforms.Length];

            for (int i = 0; i < _heldLayerTransforms.Length; i++)
            {
                GameObject go = _heldLayerTransforms[i].gameObject;
                _heldOriginalLayers[i] = go.layer;
                go.layer = layer;
            }
        }

        private void RestoreWorldLayer()
        {
            if (_heldLayerTransforms == null) return;

            for (int i = 0; i < _heldLayerTransforms.Length; i++)
                if (_heldLayerTransforms[i] != null)
                    _heldLayerTransforms[i].gameObject.layer = _heldOriginalLayers[i];

            _heldLayerTransforms = null;
            _heldOriginalLayers  = null;
        }

        // ── Arms grip ─────────────────────────────────────────────────────────────
        // Driven every frame from _heldItem instead of from each pick-up / drop /
        // release / destroy path. That also covers a held item destroyed from outside
        // (e.g. QuestItem.DestroyById) - Unity's null check catches it here - and an
        // Animator that was disabled and re-enabled, which resets its parameters.

        private void UpdateArmsGrip()
        {
            if (armsAnimator == null)
            {
                WarnGripOnce(GripIssue.NoAnimator,
                    "armsAnimator is not assigned - the arms will never change pose. " +
                    "Drag the Animator on Player/Vcam_Main/ViewmodelCamera/Arms into PlayerHand's Arms Animator field.");
                return;
            }

            if (!armsAnimator.isActiveAndEnabled)
            {
                WarnGripOnce(GripIssue.AnimatorInactive,
                    $"armsAnimator on '{armsAnimator.name}' is disabled or its GameObject is inactive.");
                return;
            }

            RuntimeAnimatorController controller = armsAnimator.runtimeAnimatorController;
            if (controller == null)
            {
                WarnGripOnce(GripIssue.NoController,
                    $"armsAnimator on '{armsAnimator.name}' has no Animator Controller assigned.");
                return;
            }

            if (controller != _gripCheckedFor)
            {
                _gripCheckedFor = controller;
                _hasGripParam   = false;
                foreach (AnimatorControllerParameter p in armsAnimator.parameters)
                {
                    if (p.nameHash == GripParam && p.type == AnimatorControllerParameterType.Int)
                    {
                        _hasGripParam = true;
                        break;
                    }
                }

                if (!_hasGripParam)
                    WarnGripOnce(GripIssue.NoParam,
                        $"Controller '{controller.name}' has no Int parameter named 'Grip'.");
            }

            if (!_hasGripParam) return;

            int grip = _heldItem != null ? GripHolding : GripEmpty;
            armsAnimator.SetInteger(GripParam, grip);
            _lastGripIssue = null;   // setup is healthy again - re-warn if it breaks later

            LogGripDiagnostics(grip);   // TEMP-GRIP-DIAG
        }

        private enum GripIssue { NoAnimator, AnimatorInactive, NoController, NoParam }

        private GripIssue? _lastGripIssue;

        /// <summary>Logs a grip setup problem once per distinct issue instead of every frame.</summary>
        private void WarnGripOnce(GripIssue issue, string message)
        {
            if (_lastGripIssue == issue) return;
            _lastGripIssue = issue;
            Debug.LogWarning($"[PlayerHand] {message}", this);
        }

        // ── TEMP-GRIP-DIAG ─────────────────────────────────────────────────────────
        // Temporary diagnostics for "the hand pose doesn't change on pick-up". Delete
        // this block and the LogGripDiagnostics call above once the cause is found
        // (search for TEMP-GRIP-DIAG).
        //
        // SetInteger runs every frame, so logging each call would flood the Console;
        // this logs the first call and every call whose value differs from the last.

        private int  _diagLastGrip = int.MinValue;
        private bool _diagBindingChecked;

        private void LogGripDiagnostics(int grip)
        {
            if (!_diagBindingChecked)
            {
                _diagBindingChecked = true;

                // The Idle/Grip clips key paths that start at "mixamorig:RightShoulder",
                // so those bones must be DIRECT children of the Animator's GameObject.
                // If this is null the clips bind to nothing and the pose never changes.
                Transform bound = armsAnimator.transform.Find("mixamorig:RightShoulder");
                Log.Info($"[PlayerHand][GripDiag] Animator GO path='{GetPath(armsAnimator.transform)}', " +
                          $"controller='{armsAnimator.runtimeAnimatorController.name}', " +
                          $"updateMode={armsAnimator.updateMode}, cullingMode={armsAnimator.cullingMode}, " +
                          $"clip path 'mixamorig:RightShoulder' resolves={(bound != null)}", this);
            }

            if (grip == _diagLastGrip) return;
            _diagLastGrip = grip;

            AnimatorStateInfo state = armsAnimator.GetCurrentAnimatorStateInfo(0);
            string stateName = state.IsName("Grip") ? "Grip" : state.IsName("Idle") ? "Idle" : $"hash {state.shortNameHash}";

            Log.Info($"[PlayerHand][GripDiag] SetInteger(\"Grip\", {grip}) - " +
                      $"animator null={armsAnimator == null}, held='{(_heldItem != null ? _heldItem.itemName : "none")}', " +
                      $"state before this frame's evaluation={stateName}, inTransition={armsAnimator.IsInTransition(0)}, " +
                      $"timeScale={Time.timeScale}", this);
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t    = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        // ── UI────────────────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            if (handStatusText == null) return;

            if (_heldItem != null)
                handStatusText.text = _heldItem.itemName;
            else if (_hoverLabel != null)
                handStatusText.text = _hoverLabel;
            else
                handStatusText.text = string.Empty;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// The collider under the crosshair, cast at most once per frame. Picking an
        /// item up or putting one down changes which colliders are active, so those
        /// paths call <see cref="InvalidateHover"/> to force a fresh cast.
        /// </summary>
        private GameObject RaycastTarget()
        {
            if (_frameHitStamp == Time.frameCount) return _frameHit;
            _frameHitStamp = Time.frameCount;

            Camera cam = Camera.main;
            if (cam == null)
            {
                _frameHit = null;
                return null;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool didHit = Physics.Raycast(ray, out RaycastHit hit, interactRange);
            _frameHit      = didHit ? hit.collider.gameObject : null;
            _frameHitPoint = didHit ? hit.point : Vector3.zero;

            return _frameHit;
        }

        /// <summary>
        /// Drops the cached ray and component lookups so the next query re-evaluates
        /// from scratch. Called whenever the contents of the hand change.
        /// </summary>
        private void InvalidateHover()
        {
            _frameHitStamp = -1;
            _resolvedFor   = null;
            _rInteractable = null;
            _rItem         = null;
            _rSecondary    = null;
            _rHoverLabel   = null;
        }
    }
}
