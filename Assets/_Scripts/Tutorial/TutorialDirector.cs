using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Walks the player through day one, one step at a time: a line of text saying
    /// what to do, and a pulsing glow on the thing to do it to.
    ///
    /// Why the steps live on a scene component and not in <see cref="TutorialData"/>:
    /// every step points at a scene object (the shutter, the fridge, the toaster),
    /// and a ScriptableObject asset cannot hold a scene reference. TutorialData keeps
    /// what it is good at — the dayı's dialogue — and the pointing lives here.
    ///
    /// Nothing is forced. A step never blocks input; it only says where to go and
    /// waits for the player to get there. Wander off, pick up the wrong thing, throw
    /// it at the wall — the step is still sitting there when they come back. That
    /// also means the tutorial can never wedge: the worst case is a line the player
    /// ignores.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        /// <summary>
        /// What finishes a step. Each one is something the game already knows.
        ///
        /// The numbers are written out and MUST NOT be renumbered. Unity serializes an
        /// enum field by its integer, so slipping a new value into the middle silently
        /// rewires every step already authored in the scene — the list keeps looking
        /// right in the inspector while each step quietly waits on the wrong thing.
        /// A new goal goes on the END with the next free number, wherever it would read
        /// most naturally in this list.
        /// </summary>
        public enum StepGoal
        {
            [InspectorName("Shutter opened")]             ShutterOpened           = 0,
            [InspectorName("Clicked the target")]         ClickTarget             = 1,
            [InspectorName("Holding the item")]           HoldItem                = 2,
            [InspectorName("Hand is empty")]              HandEmpty               = 3,
            [InspectorName("Holding bread in state")]     HoldBreadInState        = 4,
            [InspectorName("Board: whole sucuk on it")]   BoardHasSucuk           = 5,
            [InspectorName("Board: sucuk is sliced")]     BoardHasSlices          = 6,
            [InspectorName("Board: bread reached state")] BreadReachedState       = 7,
            [InspectorName("Board: bread contains")]      BreadHasIngredient      = 8,
            [InspectorName("Toaster reached state")]      ToasterReachedState     = 9,
            [InspectorName("Order items delivered")]      OrderItemsDelivered     = 10,
            [InspectorName("Tutorial order accepted")]    OrderAccepted           = 11,
            [InspectorName("Tutorial finished")]          TutorialCompleted       = 12,
            [InspectorName("Day ended")]                  DayEnded                = 13,
            [InspectorName("Just wait N seconds")]        Delay                   = 14,
            [InspectorName("Tutorial customer arrived")]  TutorialCustomerArrived = 15,
            [InspectorName("Day reached phase")]           DayPhaseReached         = 16
        }

        [Serializable]
        public class Step
        {
            [Tooltip("The line shown on screen while this step is running. Empty = show nothing.")]
            [TextArea(1, 3)]
            public string objective;

            [Tooltip("Object that pulses while this step is running. Empty = nothing glows.")]
            public GameObject highlight;

            [Tooltip("What has to happen for this step to be done.")]
            public StepGoal goal = StepGoal.ClickTarget;

            [Tooltip("For the board goals: the PrepStation. For 'Toaster reached state': the " +
                     "toaster. Leave empty to use the highlight object.")]
            public GameObject goalTarget;

            [Tooltip("For 'Board: bread reached state': the state the bread on the board has to " +
                     "reach. Open = the player right-clicked it open, Closed = folded shut again.")]
            public BreadState breadState = global::BreadState.Open;

            [Tooltip("For 'Board: bread contains': the filling that has to be in the bread.")]
            public Constants.IngredientType ingredient = Constants.IngredientType.Sucuk;

            [Tooltip("For 'Board: bread contains': how many of that filling have to be in " +
                     "there. The half loaf has two sucuk sockets and one cheese socket — ask " +
                     "for more than fits and the step waits forever.")]
            [Min(1)]
            public int ingredientCount = 1;

            [Tooltip("For 'Toaster reached state': BreadInside = loaded but the lid is still " +
                     "open, Cooking = lid shut and toasting, Done = ready to collect.")]
            public Toaster.ToasterState toasterState = Toaster.ToasterState.Cooking;

            [Tooltip("For 'Day reached phase': the phase the day has to be in. EndOfDay = " +
                     "closing time passed and the last customer is gone, so the player may leave.")]
            public DayManager.DayPhase dayPhase = DayManager.DayPhase.EndOfDay;

            [Tooltip("For 'Order items delivered': how many of the order's items have to be " +
                     "handed over. 1 = the first one, 2 = the one after that.")]
            public int deliveredCount = 1;

            [Tooltip("For 'Holding the item': the item's name as typed on its InteractableItem " +
                     "(e.g. 'Bread'). Use this for anything spawned at runtime, which cannot be " +
                     "referenced above. Empty = any item counts.")]
            public string goalItemName;

            [Tooltip("For 'Just wait N seconds': how long the line stays up before moving on.")]
            public float seconds = 3f;

            [Tooltip("Pause after this step completes, before the next line appears. Gives the " +
                     "player a beat to see that what they did worked.")]
            public float pauseAfter = 0.6f;
        }

        [Header("Wiring")]
        [Tooltip("Auto-found if left empty.")]
        [SerializeField] private DayManager        dayManager;
        [SerializeField] private OrderManager      orderManager;
        [SerializeField] private PlayerHand        playerHand;
        [SerializeField] private ObjectiveUI       objectiveUI;
        [SerializeField] private TargetHighlighter highlighter;

        [Header("Run")]
        [Tooltip("Day the guided steps run on. The büfe only needs teaching once.")]
        [SerializeField] private int runOnDay = 1;

        [Tooltip("Seconds to wait after the intro fades in before the first line appears. " +
                 "Zero feels like the game is shouting before the player has focused.")]
        [SerializeField] private float startDelay = 1.5f;

        [Header("Steps")]
        [Tooltip("Run in order, top to bottom.")]
        [SerializeField] private List<Step> steps = new List<Step>();

        // ── Runtime ──────────────────────────────────────────────────────────────

        private int   _index = -1;
        private float _stepTime;
        private float _pauseLeft;
        private bool  _running;

        // One-shot flags, cleared at the start of every step so a thing that happened
        // during step 2 cannot satisfy step 5.
        private bool       _shutterOpened;
        private bool       _orderAccepted;
        private bool       _tutorialCompleted;
        private bool       _dayEnded;
        private GameObject _lastInteracted;

        private int _delivered;             // slots of the active order filled so far

        // Resolved once per step rather than every frame.
        private Toaster     _stepToaster;
        private PrepStation _stepStation;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (dayManager   == null) dayManager   = FindAnyObjectByType<DayManager>();
            if (orderManager == null) orderManager = FindAnyObjectByType<OrderManager>();
            if (playerHand   == null) playerHand   = FindAnyObjectByType<PlayerHand>();
            if (objectiveUI  == null) objectiveUI  = FindAnyObjectByType<ObjectiveUI>();
            // FindAnyObjectByType rather than TargetHighlighter.Instance: Awake order
            // between two components is undefined, so the singleton may not be set yet.
            if (highlighter  == null) highlighter  = FindAnyObjectByType<TargetHighlighter>();
        }

        private void OnEnable()
        {
            if (dayManager != null)
            {
                dayManager.OnPhaseChanged += OnPhaseChanged;
                dayManager.OnDayEnded     += OnDayEnded;
            }

            if (orderManager != null)
            {
                orderManager.OnTutorialOrderAccepted += OnOrderAccepted;
                orderManager.OnTutorialCompleted     += OnTutorialCompleted;
                orderManager.OnOrderProgress         += OnOrderProgress;
            }

            if (playerHand != null)
                playerHand.OnInteracted += OnInteracted;
        }

        private void OnDisable()
        {
            if (dayManager != null)
            {
                dayManager.OnPhaseChanged -= OnPhaseChanged;
                dayManager.OnDayEnded     -= OnDayEnded;
            }

            if (orderManager != null)
            {
                orderManager.OnTutorialOrderAccepted -= OnOrderAccepted;
                orderManager.OnTutorialCompleted     -= OnTutorialCompleted;
                orderManager.OnOrderProgress         -= OnOrderProgress;
            }

            if (playerHand != null)
                playerHand.OnInteracted -= OnInteracted;

            // Never leave a line or a glow behind on a disabled director.
            Stop();
        }

        private void Start()
        {
            if (steps == null || steps.Count == 0) return;

            // The editor-only "Debug Skip Tutorial" toggle on OrderManager stops the
            // dayı spawning. The steps wait on him, so one checkbox has to switch off
            // both or testing wedges on "Talk to the strange man".
            if (orderManager != null && !orderManager.TutorialWillRun)
            {
                enabled = false;
                return;
            }

            if (dayManager != null && dayManager.CurrentDay != runOnDay)
            {
                // Nothing to teach today — the phase lines on ObjectiveUI carry on
                // without us.
                enabled = false;
                return;
            }

            _running   = true;
            _pauseLeft = startDelay;
            _index     = -1;
        }

        private void Update()
        {
            if (!_running) return;

            // The intro conversation owns the screen when the scene loads, cursor
            // free. Nothing starts counting until the player is actually standing in
            // the büfe — otherwise the opening delay burns down behind a panel and
            // the first line is already stale when they get there.
            if (_index < 0 && Cursor.lockState != CursorLockMode.Locked) return;

            if (_pauseLeft > 0f)
            {
                _pauseLeft -= Time.unscaledDeltaTime;
                if (_pauseLeft > 0f) return;

                Begin(_index + 1);
                return;
            }

            if (_index < 0 || _index >= steps.Count) return;

            _stepTime += Time.unscaledDeltaTime;

            if (!IsComplete(steps[_index])) return;

            // Clear the glow the moment the step is satisfied. The line stays up
            // through the pause so the player reads it as "done", not as "gone".
            if (highlighter != null) highlighter.Clear();

            _pauseLeft = Mathf.Max(0.01f, steps[_index].pauseAfter);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Ends the guided steps early and hands the line back to the day phase.</summary>
        public void Stop()
        {
            _running = false;
            _index   = steps != null ? steps.Count : 0;

            if (objectiveUI != null) objectiveUI.ClearObjective();
            if (highlighter != null) highlighter.Clear();
        }

        // ── Steps ────────────────────────────────────────────────────────────────

        private void Begin(int index)
        {
            _index    = index;
            _stepTime = 0f;

            if (_index >= steps.Count)
            {
                Debug.Log("[TutorialDirector] All steps done.");
                Stop();
                return;
            }

            Step step = steps[_index];

            // A one-shot that fired during the previous step must not carry over.
            _shutterOpened     = false;
            _orderAccepted     = false;
            _tutorialCompleted = false;
            _dayEnded          = false;
            _lastInteracted    = null;
            _delivered         = 0;   // so a delivery step waits for a hand-over that happens
                                      // DURING it, not for one the player already made

            GameObject target = Target(step);
            _stepToaster = FindOn<Toaster>(target);
            _stepStation = FindOn<PrepStation>(target);

            if (objectiveUI != null) objectiveUI.SetObjective(step.objective);
            if (highlighter != null) highlighter.SetTarget(step.highlight);

            Debug.Log($"[TutorialDirector] Step {_index + 1}/{steps.Count}: {step.objective}");
        }

        /// <summary>
        /// Finds the component the step's goal needs, starting at the wired object and
        /// searching down, then up.
        ///
        /// Deliberately forgiving. A step is authored by dragging an object out of the
        /// hierarchy, and the object that reads well as "the toaster" is rarely the exact
        /// one carrying the script — it is a visual child, or the parent holding the whole
        /// rig. Failing on that would mean a step that never completes and no clue why, on
        /// a list of eighteen of them.
        /// </summary>
        private static T FindOn<T>(GameObject root) where T : Component
        {
            if (root == null) return null;

            T found = root.GetComponentInChildren<T>(true);
            return found != null ? found : root.GetComponentInParent<T>();
        }

        private static GameObject Target(Step step) => step.goalTarget != null ? step.goalTarget : step.highlight;

        private bool IsComplete(Step step)
        {
            switch (step.goal)
            {
                case StepGoal.ShutterOpened:
                    return _shutterOpened;

                case StepGoal.ClickTarget:
                    return _lastInteracted != null && IsSameObject(_lastInteracted, Target(step));

                case StepGoal.HoldItem:
                    return HoldingWanted(step);

                case StepGoal.HandEmpty:
                    return playerHand != null && playerHand.IsHandEmpty;

                case StepGoal.HoldBreadInState:
                    // Bread and half-bread share an itemName, so a name match cannot tell
                    // them apart. The state can: Full, Half, Closed, Toasted.
                    return playerHand != null
                        && playerHand.HeldItem is PreparedItem bread
                        && bread.State == step.breadState;

                case StepGoal.BoardHasSucuk:
                    return _stepStation != null && _stepStation.HasWholeSucuk;

                case StepGoal.BoardHasSlices:
                    return _stepStation != null && _stepStation.SliceCount > 0;

                case StepGoal.BreadReachedState:
                    return _stepStation != null
                        && _stepStation.PreparedItem != null
                        && _stepStation.PreparedItem.State == step.breadState;

                case StepGoal.BreadHasIngredient:
                {
                    PreparedItem onBoard = _stepStation != null ? _stepStation.PreparedItem : null;
                    if (onBoard == null) return false;

                    // Counted rather than just looked for: one sucuk slice in an open loaf
                    // is not the same as a filled one, and a step that moves on after the
                    // first slice teaches the player to under-fill.
                    int placed = 0;
                    for (int i = 0; i < onBoard.Ingredients.Count; i++)
                        if (onBoard.Ingredients[i] == step.ingredient) placed++;

                    return placed >= step.ingredientCount;
                }

                case StepGoal.ToasterReachedState:
                    return _stepToaster != null && _stepToaster.CurrentState == step.toasterState;

                case StepGoal.OrderItemsDelivered:
                    return _delivered >= step.deliveredCount;

                case StepGoal.TutorialCustomerArrived:
                    return orderManager != null && orderManager.TutorialCustomerWaiting;

                case StepGoal.OrderAccepted:
                    return _orderAccepted;

                case StepGoal.TutorialCompleted:
                    return _tutorialCompleted;

                case StepGoal.DayPhaseReached:
                    // A level check, not an event: true for as long as the day sits in
                    // that phase, so a step can start after the phase was reached and
                    // still be satisfied.
                    return dayManager != null && dayManager.CurrentPhase == step.dayPhase;

                case StepGoal.DayEnded:
                    return _dayEnded;

                case StepGoal.Delay:
                    return _stepTime >= step.seconds;

                default:
                    return true;
            }
        }

        private bool HoldingWanted(Step step)
        {
            InteractableItem held = playerHand != null ? playerHand.HeldItem : null;
            if (held == null) return false;

            // goalTarget only, never the highlight: a HoldItem step normally glows the
            // storage the item comes OUT of, and the item in hand is not part of it.
            if (step.goalTarget != null) return IsSameObject(held.gameObject, step.goalTarget);

            if (string.IsNullOrEmpty(step.goalItemName)) return true;

            return string.Equals(held.itemName, step.goalItemName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when the two objects are the same thing as far as the player is
        /// concerned. The hand reports the object carrying the component it acted on,
        /// which can sit above or below the object wired into the step (a collider
        /// child, or a root holding the script), so the whole branch counts.
        /// </summary>
        private static bool IsSameObject(GameObject a, GameObject b)
        {
            if (a == null || b == null) return false;

            Transform ta = a.transform;
            Transform tb = b.transform;

            return ta == tb || ta.IsChildOf(tb) || tb.IsChildOf(ta);
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void OnPhaseChanged(DayManager.DayPhase phase)
        {
            if (phase == DayManager.DayPhase.Open) _shutterOpened = true;
        }

        private void OnDayEnded(int day)      => _dayEnded          = true;
        private void OnOrderAccepted()        => _orderAccepted     = true;
        private void OnTutorialCompleted()    => _tutorialCompleted = true;
        private void OnOrderProgress(int fulfilled, int total) => _delivered = fulfilled;
        private void OnInteracted(GameObject interacted) => _lastInteracted = interacted;
    }
}
