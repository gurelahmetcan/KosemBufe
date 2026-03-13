using UnityEngine;

/// <summary>
/// Orchestrates the full order-delivery loop with recipe validation:
///   1. Spawn a customer placeholder.
///   2. On click → assign a random recipe, start the timer.
///   3. Player prepares the order (slice ingredients, fill holder, toast).
///   4. On second click → validate prepared item against the recipe.
///   5. Feedback: Perfect / WrongIngredients / Raw / Burnt.
///   6. If the timer expires → order failed, customer leaves.
///   7. Reset and spawn the next customer.
///
/// Scene Setup:
///  1. Attach to a persistent GameObject (e.g. "OrderSystem").
///  2. Assign customerPrefab — a prefab with SpriteRenderer, Collider, and Customer component.
///  3. Assign spawnPoint — transform where customers appear.
///  4. Assign timerUI — the OrderTimerUI panel (starts inactive in the hierarchy).
///  5. Assign slicingController — the player's SlicingController (to read the held item).
///  6. Assign recipes — RecipeData assets that can be ordered.
/// </summary>
public class OrderManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab with SpriteRenderer, Collider, and Customer component.")]
    public Customer customerPrefab;

    [Tooltip("World-space position where new customers appear.")]
    public Transform spawnPoint;

    [Tooltip("Circular countdown UI (top-left of screen).")]
    public OrderTimerUI timerUI;

    [Tooltip("Player's SlicingController — used to read the item in hand for validation.")]
    public SlicingController slicingController;

    [Header("Settings")]
    [Tooltip("How many seconds the player has to deliver the order.")]
    public float orderDuration = 30f;

    [Tooltip("Available recipes — one is picked at random per customer.")]
    public RecipeData[] recipes;

    [Header("Input")]
    [Tooltip("Key to simulate picking up the prepared item (dev shortcut).")]
    public KeyCode pickUpKey = KeyCode.B;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private Customer _activeCustomer;
    private bool     _isHoldingOrder;
    private bool     _orderActive;

    // ── Unity loop ───────────────────────────────────────────────────────────

    private void Start()
    {
        SpawnCustomer();
    }

    private void Update()
    {
        HandlePickUpInput();
        HandleClickInput();
        CheckTimerExpiry();
    }

    // ── Input handling ───────────────────────────────────────────────────────

    private void HandlePickUpInput()
    {
        if (Input.GetKeyDown(pickUpKey))
        {
            _isHoldingOrder = true;
            Debug.Log("Order picked up (B pressed). isHoldingOrder = true");
        }
    }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (_activeCustomer == null) return;

        // Raycast to see if the player clicked the customer
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        Customer clicked = hit.collider.GetComponent<Customer>()
                        ?? hit.collider.GetComponentInParent<Customer>();

        if (clicked == null || clicked != _activeCustomer) return;

        switch (_activeCustomer.CurrentState)
        {
            case Customer.State.WaitingForOrder:
                StartOrder();
                break;

            case Customer.State.WaitingForFood:
                TryDeliver();
                break;
        }
    }

    // ── Timer expiry ─────────────────────────────────────────────────────────

    private void CheckTimerExpiry()
    {
        if (!_orderActive) return;
        if (timerUI == null || timerUI.Remaining > 0f) return;

        Debug.Log("Order Failed: CUSTOMER LEFT");
        EndOrder();
    }

    // ── Order flow ───────────────────────────────────────────────────────────

    private void StartOrder()
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogError("[OrderManager] No recipes assigned!", this);
            return;
        }

        RecipeData recipe = recipes[Random.Range(0, recipes.Length)];
        _activeCustomer.AssignOrder(recipe);
        _orderActive = true;

        if (timerUI != null)
            timerUI.StartTimer(orderDuration);

        Debug.Log($"Order started: {recipe.recipeName} — {orderDuration}s on the clock");
    }

    private void TryDeliver()
    {
        // Check if the player is holding anything (real item or B-key shortcut)
        HoldableItem heldItem = slicingController != null ? slicingController.HeldItem : null;
        bool hasItem = heldItem != null || _isHoldingOrder;

        if (!hasItem)
        {
            Debug.Log("Cannot deliver — not holding anything!");
            return;
        }

        RecipeData recipe = _activeCustomer.Recipe;

        // If using B-key shortcut (no real item), auto-succeed for testing
        if (heldItem == null && _isHoldingOrder)
        {
            Debug.Log("Order Delivered: SUCCESS (B-key shortcut)");
            _isHoldingOrder = false;
            EndOrder();
            return;
        }

        // Validate the held item against the recipe
        OrderResult result = OrderValidator.Validate(
            recipe,
            heldItem.preparedIngredients,
            heldItem.cookState
        );

        switch (result)
        {
            case OrderResult.Perfect:
                Debug.Log($"Order Delivered: PERFECT! ({recipe.recipeName})");
                slicingController.DestroyHeldItem();
                break;

            case OrderResult.WrongIngredients:
                Debug.Log($"Order Failed: WRONG INGREDIENTS for {recipe.recipeName}");
                break;

            case OrderResult.Raw:
                Debug.Log($"Order Failed: RAW — {recipe.recipeName} needs toasting!");
                break;

            case OrderResult.Burnt:
                Debug.Log($"Order Failed: BURNT — {recipe.recipeName} was overcooked!");
                break;
        }

        _isHoldingOrder = false;
        EndOrder();
    }

    private void EndOrder()
    {
        _orderActive = false;

        if (timerUI != null)
            timerUI.StopTimer();

        if (_activeCustomer != null)
        {
            Destroy(_activeCustomer.gameObject);
            _activeCustomer = null;
        }

        // Spawn the next customer after a short delay
        Invoke(nameof(SpawnCustomer), 1.5f);
    }

    // ── Spawning ─────────────────────────────────────────────────────────────

    private void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[OrderManager] customerPrefab or spawnPoint is not assigned!", this);
            return;
        }

        _activeCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        _isHoldingOrder = false;
        Debug.Log("New customer spawned.");
    }
}
