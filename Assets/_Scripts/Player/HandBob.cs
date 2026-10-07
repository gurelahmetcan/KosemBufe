using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Bufe
{
    /// <summary>
    /// Procedural walk bob for the first-person arms. Sways each Two Bone IK target
    /// on a sine wave while the player walks; the rig then pulls the arms (and
    /// anything parented to the hand, like a held item) along with them.
    ///
    /// Shape: X swings at the step frequency, Y at twice that (the classic head-bob
    /// figure-eight). The right hand is the left hand's mirror (-x, -y). A plain
    /// half-cycle phase shift would not do it: at double frequency it puts Y back
    /// in phase, so both hands would still bounce up and down together.
    ///
    /// Amplitude follows walking speed and eases to zero when the player stops or
    /// leaves the ground, so the motion never cuts off abruptly. The right hand
    /// uses <see cref="heldMultiplier"/> while it holds an item.
    ///
    /// The wave is built in this object's view space (right/up) in world units and
    /// converted into the moved Transform's parent space, so bobAmount means the
    /// same distance on screen whatever the rig's import scale is.
    ///
    /// What actually gets moved is each target's "pivot" - see <see cref="ArmsRig"/>
    /// for why the IK target itself can't be written to.
    ///
    /// While <see cref="HandReach"/> drives the right hand (reach + return), the bob
    /// leaves that hand alone and restarts its amplitude from zero afterwards, so
    /// the hand eases back into the bob instead of jumping. The left hand keeps
    /// bobbing - nothing else writes to it.
    ///
    /// Why Update, not LateUpdate: the rig solves inside the Animator's evaluation,
    /// which runs after every Update and before any LateUpdate. A value written in
    /// Update is used the same frame; one written in LateUpdate arrives a frame
    /// late, and could not beat a clip either.
    ///
    /// Setup: put this on the object with the Animator + Rig Builder. Leave the
    /// target fields empty to take them from the Two Bone IK constraints.
    /// </summary>
    public class HandBob : MonoBehaviour
    {
        [Header("IK Targets")]
        [Tooltip("Left IK target. Auto-found.")]
        [SerializeField] private Transform leftHandTarget;

        [Tooltip("Right IK target. Auto-found.")]
        [SerializeField] private Transform rightHandTarget;

        [Header("Player")]
        [Tooltip("Drives grounded check and bob. Auto-found.")]
        [SerializeField] private CharacterController characterController;

        [Tooltip("Its moveSpeed = full bob. Auto-found.")]
        [SerializeField] private PlayerMovement playerMovement;

        [Tooltip("Holding state source. Auto-found.")]
        [SerializeField] private PlayerHand playerHand;

        [Tooltip("Its hand is skipped while reaching. Auto-found.")]
        [SerializeField] private HandReach handReach;

        [Header("Bob")]
        [Tooltip("Step rate (rad/s) at full speed.")]
        [SerializeField] private float bobSpeed = 10f;

        [Tooltip("Bob height (m) at full speed.")]
        [SerializeField] private float bobAmount = 0.015f;

        [Tooltip("Sway as a fraction of bobAmount.")]
        [SerializeField] private float horizontalRatio = 0.5f;

        [Tooltip("Right-hand amplitude while holding (<1 = heavier).")]
        [SerializeField] private float heldMultiplier = 0.6f;

        [Tooltip("Amplitude ease speed.")]
        [SerializeField] private float smooth = 8f;

        // ── Per-hand runtime ─────────────────────────────────────────────────────

        private sealed class Hand
        {
            public string    side;
            public Transform pivot;      // what we actually move
            public Vector3   basePos;    // pivot's local position at start
            public Vector3   written;    // what Update wrote this frame
            public bool      wrote;      // whether Update wrote it this frame
            public float     amp;
        }

        private readonly Hand _left  = new Hand { side = "Left"  };
        private readonly Hand _right = new Hand { side = "Right" };

        private bool    _ready;
        private float   _phase;
        private Vector3 _lastBodyPos;
        private bool    _hasLastBodyPos;
        private bool    _wroteThisFrame;
        private bool    _overwriteWarned;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (characterController == null) characterController = GetComponentInParent<CharacterController>();
            if (playerMovement      == null) playerMovement      = GetComponentInParent<PlayerMovement>();
            if (playerHand          == null) playerHand          = GetComponentInParent<PlayerHand>();
        }

        private void Start()
        {
            // In Start, not Awake: PlayerHand may add a HandReach at runtime in its own
            // Awake, which can run after this object's Awake.
            if (handReach == null) handReach = GetComponent<HandReach>();

            Animator animator = GetComponentInParent<Animator>();
            TwoBoneIKConstraint[] constraints = GetComponentsInChildren<TwoBoneIKConstraint>(true);

            ArmsRig.ResolveTargets(constraints, ref leftHandTarget, ref rightHandTarget, this);

            bool addedMarker = false;
            _left.pivot  = ArmsRig.ChoosePivot(leftHandTarget,  animator, constraints, ref addedMarker);
            _right.pivot = ArmsRig.ChoosePivot(rightHandTarget, animator, constraints, ref addedMarker);
            ArmsRig.RebuildIfNeeded(animator, addedMarker);

            if (_left.pivot  != null) _left.basePos  = _left.pivot.localPosition;
            if (_right.pivot != null) _right.basePos = _right.pivot.localPosition;
            _ready = true;

            LogStartupReport();   // TEMP-HANDBOB-DIAG
        }

        private void OnDisable()
        {
            // Don't leave the hands frozen mid-swing.
            if (!_ready) return;
            if (_left.pivot  != null && !IsReached(_left))  _left.pivot.localPosition  = _left.basePos;
            if (_right.pivot != null && !IsReached(_right)) _right.pivot.localPosition = _right.basePos;

            _left.amp = _right.amp = 0f;
            _left.wrote = _right.wrote = false;
            _hasLastBodyPos = false;
            _wroteThisFrame = false;
        }

        private void Update()
        {
            _wroteThisFrame = false;
            _left.wrote = _right.wrote = false;

            if (!_ready || characterController == null) return;
            if (_left.pivot == null && _right.pivot == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;   // paused (timeScale 0) - hold the current pose

            float speed01  = MeasureSpeed01(dt);
            bool  grounded = characterController.isGrounded;
            float target   = grounded ? speed01 : 0f;

            bool  holding   = playerHand != null && !playerHand.IsHandEmpty;
            float rightMult = holding ? heldMultiplier : 1f;

            // Frame-rate independent exponential ease: 1 - e^(-k·dt).
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, smooth) * dt);
            _left.amp  = Mathf.Lerp(_left.amp,  target,             t);
            _right.amp = Mathf.Lerp(_right.amp, target * rightMult, t);

            // Steps slow down along with the player. Wrapped to keep sin() precise
            // on long sessions.
            _phase = Mathf.Repeat(_phase + bobSpeed * speed01 * dt, Mathf.PI * 2f);

            // World-space wave along the view's axes (unit directions, so this
            // object's own scale doesn't shrink it).
            Vector3 wave = transform.right * (Mathf.Sin(_phase) * bobAmount * horizontalRatio)
                         + transform.up    * (Mathf.Sin(_phase * 2f) * bobAmount);

            // HandReach owns a hand while it reaches: don't write it, and restart its
            // amplitude from zero so it eases back into the bob afterwards.
            if (IsReached(_left))  _left.amp  = 0f;
            if (IsReached(_right)) _right.amp = 0f;

            Apply(_left,   wave * _left.amp);
            Apply(_right, -wave * _right.amp);   // mirrored: opposite phase on both axes

            _wroteThisFrame = true;

            LogMotion(speed01, grounded, wave.magnitude);   // TEMP-HANDBOB-DIAG
        }

        private void LateUpdate()
        {
            // Nothing else should touch a pivot HandBob wrote; if one moved between
            // Update and now, an Animator clip is keyframing it and this bob never
            // reaches the IK. (Hands HandReach owns are not written, so not checked.)
            if (!_wroteThisFrame || _overwriteWarned) return;

            Hand hit = WasOverwritten(_left) ? _left : WasOverwritten(_right) ? _right : null;
            if (hit == null) return;

            _overwriteWarned = true;
            Debug.LogWarning($"[HandBob] {hit.side} bob pivot '{hit.pivot.name}' was overwritten after Update - " +
                             "an Animator clip keyframes it, so its bob is discarded. Remove its position keys, " +
                             "or keep the IK target under an ancestor that no clip animates.", this);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void Apply(Hand hand, Vector3 worldOffset)
        {
            if (hand.pivot == null || IsReached(hand)) return;

            hand.written = hand.basePos + ArmsRig.ToParentSpace(hand.pivot, worldOffset);
            hand.pivot.localPosition = hand.written;
            hand.wrote = true;
        }

        /// <summary>True while HandReach is moving this hand's pivot.</summary>
        private bool IsReached(Hand hand)
            => handReach != null && handReach.IsReaching && hand.pivot != null && handReach.Pivot == hand.pivot;

        // A reach can start later in the same frame (PlayerHand's click may run after
        // this Update) and move the pivot - that's a hand-over, not a clip overwriting.
        private bool WasOverwritten(Hand hand)
            => hand.wrote && hand.pivot != null && !IsReached(hand)
               && (hand.pivot.localPosition - hand.written).sqrMagnitude > 1e-10f;

        /// <summary>
        /// Horizontal body speed as a 0–1 fraction of the walk speed, measured from the
        /// body's actual movement. CharacterController.velocity would go stale whenever
        /// PlayerMovement stops calling Move (UI open, cash register).
        /// </summary>
        private float MeasureSpeed01(float dt)
        {
            Vector3 pos = characterController.transform.position;

            if (!_hasLastBodyPos)
            {
                _lastBodyPos    = pos;
                _hasLastBodyPos = true;
                return 0f;
            }

            Vector3 delta = pos - _lastBodyPos;
            _lastBodyPos = pos;
            delta.y = 0f;

            float maxSpeed = playerMovement != null && playerMovement.moveSpeed > 0f ? playerMovement.moveSpeed : 4f;
            float speed    = delta.magnitude / dt;

            // A jump far beyond walking pace is a teleport (day reset, stepping up to
            // the register), not a stride - don't let it kick the hands.
            if (speed > maxSpeed * 3f) return 0f;

            return Mathf.Clamp01(speed / maxSpeed);
        }

        // ── TEMP-HANDBOB-DIAG ─────────────────────────────────────────────────────
        // Temporary diagnostics. Delete this block and the two calls marked
        // TEMP-HANDBOB-DIAG once the bob is confirmed working.

        private float _diagNextLog;

        private void LogStartupReport()
        {
            string Describe(Transform target, Hand hand) => target == null ? "NONE" :
                $"target '{target.name}' → moving '{(hand.pivot != null ? hand.pivot.name : "-")}'" +
                $"{(hand.pivot != null && hand.pivot.GetComponent<RigTransform>() != null ? " [RigTransform]" : "")}";

            Log.Info($"[HandBob][Diag] on '{name}': left: {Describe(leftHandTarget, _left)}, " +
                      $"right: {Describe(rightHandTarget, _right)}, " +
                      $"controller={(characterController != null ? characterController.name : "NONE")}, " +
                      $"movement={(playerMovement != null ? "ok" : "NONE")}, hand={(playerHand != null ? "ok" : "NONE")}", this);
        }

        private void LogMotion(float speed01, bool grounded, float waveWorld)
        {
            if (Time.unscaledTime < _diagNextLog) return;
            _diagNextLog = Time.unscaledTime + 1f;

            if (speed01 <= 0.01f && _left.amp <= 0.01f && _right.amp <= 0.01f) return;   // idle: stay quiet

            Log.Info($"[HandBob][Diag] speed01={speed01:0.00}, grounded={grounded}, ampL={_left.amp:0.00}, " +
                      $"ampR={_right.amp:0.00}, wave={waveWorld * 1000f:0.0} mm world", this);
        }
    }
}
