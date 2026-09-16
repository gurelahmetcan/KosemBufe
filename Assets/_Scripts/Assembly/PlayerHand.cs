using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Bufe
{
    public class PlayerHand : MonoBehaviour
    {
        [Header("Hold Point")]
        [Tooltip("Empty child of the Camera where held items are positioned and displayed.")]
        public Transform holdPoint;

        [Header("Input")]
        [Tooltip("Maximum raycast distance for clicking on items and stations.")]
        public float interactRange = 10f;

        [Tooltip("Forward force applied to the item when thrown (right-click held).")]
        public float throwForce = 5f;

        [Tooltip("How long the right mouse button must be held before it counts as a throw instead of a drop.")]
        public float throwHoldThreshold = 0.25f;

        [Header("Drop")]
        [Tooltip("Safety net: how long a released item may ignore the player before " +
                 "collisions are restored regardless of where it ended up.")]
        public float releaseIgnoreTimeout = 2f;

        [Header("UI")]
        [Tooltip("Optional TextMeshProUGUI showing what the player is currently holding.")]
        public TextMeshProUGUI handStatusText;

        /// <summary>Layer the Viewmodelcamera renders — the arms and whatever they hold.</summary>
        private const string ViewmodelLayer = "Viewmodel";

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
        private int        _frameHitStamp = -1;    // frame the cached ray was cast on

        private GameObject             _resolvedFor;   // object the lookups below describe
        private IAssemblyInteractable  _rInteractable;
        private InteractableItem       _rItem;
        private ISecondaryInteractable _rSecondary;
        private IHoverLabel            _rHoverLabel;

        private GameObject _outlinedRoot;          // currently outlined object, null when none

        // The player's own colliders — the CharacterController capsule among them.
        // A dropped item ignores these until it has fallen clear of them.
        private Collider[] _selfColliders;

        // ── Public API ───────────────────────────────────────────────────────────

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

        private void Awake() => _selfColliders = transform.root.GetComponentsInChildren<Collider>(true);

        private void Update()
        {
            if (InputBlocked)
            {
                // A station is driving the player (e.g. an appliance is processing).
                // Drop the outline too — it would otherwise sit there inviting a
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

            if (!ResolveHover()) return;

            if (_rInteractable != null && _rInteractable.CanInteract(this))
            {
                _rInteractable.Interact(this);
                return;
            }

            if (_heldItem == null && _rItem != null)
                PickUp(_rItem);
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
        /// when nothing under the crosshair is usable right now — a fridge door while
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
            // Custom labels first (e.g. storages showing "Name — N left"). A component
            // that implements the interface but leaves the label empty falls through to
            // the generic names below rather than leaving the player with nothing.
            if (_rHoverLabel != null)
            {
                string custom = _rHoverLabel.HoverLabel;
                if (!string.IsNullOrEmpty(custom)) return custom;
            }

            if (_rItem != null) return _rItem.itemName;

            if (_rInteractable != null) return _resolvedFor.name;

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

            item.transform.SetParent(holdPoint);
            item.transform.localPosition = item.InHandLocalPos;
            item.transform.localRotation = item.InHandLocalRotation;

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
        // the "flung forwards and left hanging in mid-air" drop — the throw force has
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

        // ── UI ────────────────────────────────────────────────────────────────────

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
            _frameHit = Physics.Raycast(ray, out RaycastHit hit, interactRange)
                ? hit.collider.gameObject
                : null;

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
