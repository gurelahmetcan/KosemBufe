using UnityEngine;

/// <summary>
/// 3D animated customer that walks to the counter, waves until the player
/// takes an order, idles while waiting for food, then walks to an exit.
///
/// States:
///   ARRIVING         → Walking from spawn to counter (Walking anim).
///   WAITING_FOR_ORDER → Standing at counter, waving (Wave anim).
///   SHOWING_ORDER     → Order prompt is visible (Wave anim continues).
///   WAITING_FOR_FOOD  → Order accepted, waiting for delivery (Idle anim).
///   LEAVING           → Walking from counter to exit (Walking anim), self-destroys on arrival.
///
/// </summary>

namespace Bufe
{
    public class Customer : MonoBehaviour
    {
        public enum State { Arriving, WaitingForOrder, ShowingOrder, WaitingForFood, Leaving }

        [Header("Animation")]
        [Tooltip("Animator on the 3D model child. Needs bool params: IsWalking, IsWaving.")]
        public Animator animator;

        [Header("Movement")]
        [Tooltip("Walk speed in units per second.")]
        public float walkSpeed = 2f;

        // ── Animator parameter hashes ────────────────────────────────────────────

        private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
        private static readonly int AnimIsWaving  = Animator.StringToHash("IsWaving");

        // ── Public state ─────────────────────────────────────────────────────────

        public State            CurrentState { get; private set; } = State.Arriving;
        public Order       Recipe       { get; private set; }
        public CustomerInstance Instance     { get; private set; }

        /// <summary>Non-null when this customer is a scripted faction member (demo: dialogue/choice, no cooking).</summary>
        public FactionInteraction FactionInteraction { get; private set; }

        /// <summary>True when this customer is a faction member rather than a normal order customer.</summary>
        public bool IsFactionMember => FactionInteraction != null;

        /// <summary>True when this customer is the scripted day-1 tutorial visitor.</summary>
        public bool IsTutorial { get; private set; }

        // ── Movement runtime ─────────────────────────────────────────────────────

        private Vector3       _moveTarget;
        private System.Action _onArrivedCallback;
        private bool          _isMoving;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises this customer with a resolved instance.
        /// Called by OrderManager right after instantiation.
        /// </summary>
        public void Initialise(CustomerInstance instance)
        {
            Instance = instance;
            Recipe   = instance.Order;
        }

        /// <summary>
        /// Initialises this customer as a scripted faction member. No CustomerInstance/recipe —
        /// the visit is dialogue/choice only (demo scope).
        /// </summary>
        public void InitialiseFaction(FactionInteraction interaction)
        {
            FactionInteraction = interaction;
        }

        /// <summary>
        /// Initialises this customer as the scripted day-1 tutorial visitor. There is no
        /// CustomerInstance/mood — dialogue comes from TutorialData and the order is fixed.
        /// </summary>
        public void InitialiseTutorial(Order order)
        {
            IsTutorial = true;
            Recipe     = order;
        }

        /// <summary>
        /// Enters the showing state for a faction interaction (keeps waving while the
        /// choice prompt is open). Faction members never transition to WaitingForFood.
        /// </summary>
        public void ShowFactionInteraction()
        {
            CurrentState = State.ShowingOrder;
        }

        /// <summary>
        /// Begins walking from the current position toward the counter.
        /// On arrival the customer starts waving and enters WaitingForOrder.
        /// </summary>
        public void StartArriving(Vector3 counterPos, Quaternion counterRotation,
                                  System.Action onArrived = null)
        {
            CurrentState = State.Arriving;
            WalkTo(counterPos, () =>
            {
                transform.rotation = counterRotation;
                CurrentState = State.WaitingForOrder;
                SetAnimWaving(true);
                onArrived?.Invoke();
            });
        }

        /// <summary>
        /// Shows the order prompt — the recipe is stored but the timer has not started yet.
        /// Called by OrderManager when the player first clicks this customer.
        /// </summary>
        public void ShowOrder(Order recipe)
        {
            Recipe       = recipe;
            CurrentState = State.ShowingOrder;
            // Keep waving while the prompt is visible
        }

        /// <summary>
        /// Accepts the order and transitions to WaitingForFood (timer starts).
        /// Called by OrderManager when the player clicks Accept on the prompt.
        /// </summary>
        public void AcceptOrder()
        {
            CurrentState = State.WaitingForFood;
            SetAnimWaving(false); // → Idle
        }

        /// <summary>
        /// Assigns a recipe to the customer and transitions to WaitingForFood.
        /// </summary>
        public void AssignOrder(Order recipe)
        {
            Recipe       = recipe;
            CurrentState = State.WaitingForFood;
            SetAnimWaving(false); // → Idle
        }

        /// <summary>
        /// Resets the customer to WaitingForOrder (e.g. after closing the prompt).
        /// </summary>
        public void ResetCustomer()
        {
            CurrentState = State.WaitingForOrder;
            Recipe       = null;
            SetAnimWaving(true);
        }

        /// <summary>
        /// Begins walking from the current position toward the exit.
        /// Self-destroys on arrival.
        /// </summary>
        public void StartLeaving(Vector3 exitPos)
        {
            CurrentState = State.Leaving;
            SetAnimWaving(false);

            WalkTo(exitPos, () => Destroy(gameObject));
        }

        // ── Movement ─────────────────────────────────────────────────────────────

        private void WalkTo(Vector3 target, System.Action onArrived = null)
        {
            _moveTarget        = target;
            _onArrivedCallback = onArrived;
            _isMoving          = true;

            // Face the walk direction
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);

            SetAnimWalking(true);
        }

        private void Update()
        {
            if (!_isMoving) return;

            transform.position = Vector3.MoveTowards(
                transform.position, _moveTarget, walkSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _moveTarget) < 0.05f)
            {
                _isMoving = false;
                SetAnimWalking(false);

                System.Action cb = _onArrivedCallback;
                _onArrivedCallback = null;
                cb?.Invoke();
            }
        }

        // ── Animation helpers ────────────────────────────────────────────────────

        private void SetAnimWalking(bool walking)
        {
            if (animator != null) animator.SetBool(AnimIsWalking, walking);
        }

        private void SetAnimWaving(bool waving)
        {
            if (animator != null) animator.SetBool(AnimIsWaving, waving);
        }
    }

}
