using UnityEngine;

/// <summary>
/// Orchestrates the full order-delivery loop:
///   1. Spawn a customer placeholder.
///   2. On click → assign a random order, start the timer.
///   3. Player presses B to simulate picking up the prepared item.
///   4. On second click (while holding) → deliver the order (success).
///   5. If the timer expires → order failed, customer leaves.
///   6. Reset and spawn the next customer.
///
/// Scene Setup:
///  1. Attach to a persistent GameObject (e.g. "OrderSystem").
///  2. Assign customerPrefab — a prefab with SpriteRenderer, Collider, and Customer component.
///  3. Assign spawnPoint — transform where customers appear.
///  4. Assign timerUI — the OrderTimerUI panel (starts inactive in the hierarchy).
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

    [Header("Settings")]
    [Tooltip("How many seconds the player has to deliver the order.")]
    public float orderDuration = 30f;

    [Tooltip("Available order names — one is picked at random per customer.")]
    public string[] possibleOrders = { "Tost" };

    [Header("Input")]
    [Tooltip("Key to simulate picking up the prepared item.")]
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
        string order = possibleOrders[Random.Range(0, possibleOrders.Length)];
        _activeCustomer.AssignOrder(order);
        _orderActive = true;

        if (timerUI != null)
            timerUI.StartTimer(orderDuration);

        Debug.Log($"Order started: {order} — {orderDuration}s on the clock");
    }

    private void TryDeliver()
    {
        if (!_isHoldingOrder)
        {
            Debug.Log("Cannot deliver — not holding an order! Press B first.");
            return;
        }

        Debug.Log("Order Delivered: SUCCESS");
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
