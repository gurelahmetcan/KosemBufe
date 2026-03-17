using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Orchestrates the full order-delivery loop with recipe validation:
///   1. Spawn a customer placeholder.
///   2. On click → assign a random recipe, start the timer.
///   3. Player prepares the order (slice ingredients, fill holder, toast).
///   4. On second click → validate prepared item against the recipe.
///   5. Feedback: Perfect / WrongIngredients / Raw / Burnt.
///   6. If the timer expires → order failed, customer leaves.
///   7. Reset and spawn the next customer.
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

    [FormerlySerializedAs("slicingController")] [Tooltip("Player's SlicingController — used to read the item in hand for validation.")]
    public InHandController ınHandController;

    [Tooltip("Screen-space prompt UI with Accept / Reject / Close buttons.")]
    public OrderPromptUI promptUI;

    [Tooltip("Player camera — rotation is disabled while the prompt is visible.")]
    public PlayerCameraRotation playerCamera;

    [Header("Settings")]
    [Tooltip("How many seconds the customer waits before leaving if ignored.")]
    public float patienceDuration = 15f;

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
        if (promptUI != null)
        {
            promptUI.OnAccept += OnPromptAccept;
            promptUI.OnReject += OnPromptReject;
            promptUI.OnClose  += OnPromptClose;
        }

        SpawnCustomer();
    }

    private void OnDestroy()
    {
        if (promptUI != null)
        {
            promptUI.OnAccept -= OnPromptAccept;
            promptUI.OnReject -= OnPromptReject;
            promptUI.OnClose  -= OnPromptClose;
        }
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

        // Ignore clicks while the prompt is already showing
        if (promptUI != null && promptUI.IsVisible) return;

        // Raycast to see if the player clicked the customer
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        Customer clicked = hit.collider.GetComponent<Customer>()
                        ?? hit.collider.GetComponentInParent<Customer>();

        if (clicked == null || clicked != _activeCustomer) return;

        switch (_activeCustomer.CurrentState)
        {
            case Customer.State.WaitingForOrder:
                ShowOrderPrompt();
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

    // ── Order prompt ────────────────────────────────────────────────────────

    private void ShowOrderPrompt()
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogError("[OrderManager] No recipes assigned!", this);
            return;
        }

        RecipeData recipe = recipes[Random.Range(0, recipes.Length)];
        _activeCustomer.ShowOrder(recipe);

        // Pause patience timer while player is deciding
        if (_activeCustomer.patienceTimer != null)
            _activeCustomer.patienceTimer.Pause();

        if (promptUI != null)
            promptUI.Show(recipe.recipeName);

        SetCameraLocked(true);

        Debug.Log($"Showing order prompt: {recipe.recipeName}");
    }

    private void OnPromptAccept()
    {
        SetCameraLocked(false);

        if (_activeCustomer == null) return;

        // Patience timer is no longer needed — order was taken
        if (_activeCustomer.patienceTimer != null)
            _activeCustomer.patienceTimer.Stop();

        _activeCustomer.AcceptOrder();
        _orderActive = true;

        if (timerUI != null)
            timerUI.StartTimer(orderDuration);

        Debug.Log($"Order accepted: {_activeCustomer.Recipe.recipeName} — {orderDuration}s on the clock");
    }

    private void OnPromptReject()
    {
        SetCameraLocked(false);

        Debug.Log("Order rejected — customer leaves.");

        if (_activeCustomer != null)
        {
            Destroy(_activeCustomer.gameObject);
            _activeCustomer = null;
        }

        Invoke(nameof(SpawnCustomer), 1.5f);
    }

    private void OnPromptClose()
    {
        SetCameraLocked(false);

        // Return customer to WaitingForOrder so the player can click again later
        if (_activeCustomer != null)
        {
            _activeCustomer.ResetCustomer();

            // Resume patience timer — it continues from where it was paused
            if (_activeCustomer.patienceTimer != null)
                _activeCustomer.patienceTimer.Resume();
        }

        Debug.Log("Order prompt closed — customer still waiting.");
    }

    private void OnPatienceExpired()
    {
        Debug.Log("Customer lost patience and left! (TODO: -7 Reputation)");

        // If the prompt is open, close it
        if (promptUI != null && promptUI.IsVisible)
            promptUI.Hide();

        SetCameraLocked(false);

        if (_activeCustomer != null)
        {
            Destroy(_activeCustomer.gameObject);
            _activeCustomer = null;
        }

        Invoke(nameof(SpawnCustomer), 1.5f);
    }

    private void SetCameraLocked(bool locked)
    {
        if (playerCamera != null)
            playerCamera.InputEnabled = !locked;
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
        HoldableItem heldItem = ınHandController != null ? ınHandController.HeldItem : null;
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

        // Always destroy the delivered item and clear the hand
        ınHandController.DestroyHeldItem();
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

        // Start the patience timer
        if (_activeCustomer.patienceTimer != null)
        {
            _activeCustomer.patienceTimer.OnExpired += OnPatienceExpired;
            _activeCustomer.patienceTimer.StartTimer(patienceDuration);
        }

        Debug.Log("New customer spawned.");
    }
}
