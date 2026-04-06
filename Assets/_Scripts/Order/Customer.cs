using UnityEngine;
using TMPro;

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
/// Scene Setup:
///   1. Attach to a GameObject with a Collider and child 3D model.
///   2. Add an Animator with bool parameters "IsWalking" and "IsWaving".
///   3. Assign orderLabel – a child TextMeshPro (world-space) that shows
///      the order name above the customer. Starts disabled.
/// </summary>
public class Customer : MonoBehaviour
{
    public enum State { Arriving, WaitingForOrder, ShowingOrder, WaitingForFood, Leaving }

    [Header("UI")]
    [Tooltip("World-space TextMeshPro label shown above the customer after ordering.")]
    public TextMeshPro orderLabel;

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
    public RecipeData       Recipe       { get; private set; }
    public CustomerInstance Instance     { get; private set; }

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
    public void ShowOrder(RecipeData recipe)
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

        if (orderLabel != null && Recipe != null)
        {
            orderLabel.text = Recipe.recipeName;
            orderLabel.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Assigns a recipe to the customer and transitions to WaitingForFood.
    /// </summary>
    public void AssignOrder(RecipeData recipe)
    {
        Recipe       = recipe;
        CurrentState = State.WaitingForFood;
        SetAnimWaving(false); // → Idle

        if (orderLabel != null)
        {
            orderLabel.text = recipe.recipeName;
            orderLabel.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Resets the customer to WaitingForOrder (e.g. after closing the prompt).
    /// </summary>
    public void ResetCustomer()
    {
        CurrentState = State.WaitingForOrder;
        Recipe       = null;
        SetAnimWaving(true);

        if (orderLabel != null)
            orderLabel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Begins walking from the current position toward the exit.
    /// Self-destroys on arrival.
    /// </summary>
    public void StartLeaving(Vector3 exitPos)
    {
        CurrentState = State.Leaving;
        SetAnimWaving(false);

        if (orderLabel != null)
            orderLabel.gameObject.SetActive(false);

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
