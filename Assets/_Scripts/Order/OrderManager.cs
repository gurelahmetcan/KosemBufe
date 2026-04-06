using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Orchestrates the full order-delivery loop for normal customers:
///
///   1. CustomerFactory creates a CustomerInstance (archetype + mood + order + modifier).
///   2. Customer spawns → opening line shown via customer UI.
///   3. Player clicks customer → order line (+ optional modifier) shown, prompt appears.
///   4. Player accepts → prepares the order (slice, fill, toast).
///   5. Player clicks customer with prepared item → validate (recipe + modifier).
///   6. Result → positive/negative exit line shown, customer leaves.
///   7. Timer expires → timeout exit line shown, customer leaves.
///
/// Spawning is driven by DayManager: customers only arrive while the shop is open.
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

    [FormerlySerializedAs("slicingController")]
    [Tooltip("Player's InHandController — used to read the item in hand for validation.")]
    public InHandController ınHandController;

    [Tooltip("Unified customer UI — dialogue bubbles + accept/reject/close prompt.")]
    public CustomerOrderUI customerUI;

    [Tooltip("Player camera — rotation is disabled while the prompt is visible.")]
    public PlayerCameraRotation playerCamera;

    [Tooltip("Reputation tracker — receives score changes from order outcomes.")]
    public ReputationManager reputationManager;

    [Tooltip("Factory that creates customer instances from archetype pool.")]
    public CustomerFactory customerFactory;

    [Tooltip("Day manager — controls when customers can spawn.")]
    public DayManager dayManager;

    [Tooltip("Money tracker — receives income from delivered orders.")]
    public MoneyManager moneyManager;

    [Header("Settings")]
    [Tooltip("How many seconds the customer waits before leaving if ignored.")]
    public float patienceDuration = 15f;

    [Tooltip("How many seconds the player has to deliver the order.")]
    public float orderDuration = 30f;

    [Tooltip("Fallback recipes — used only if CustomerFactory is not assigned.")]
    public RecipeData[] recipes;

    [Header("Input")]
    [Tooltip("Key to simulate picking up the prepared item (dev shortcut).")]
    public Key pickUpKey = Key.B;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private Customer _activeCustomer;
    private bool     _isHoldingOrder;
    private bool     _orderActive;

    // ── Unity loop ───────────────────────────────────────────────────────────

    private void Start()
    {
        if (customerUI != null)
        {
            customerUI.OnAccept += OnPromptAccept;
            customerUI.OnReject += OnPromptReject;
            customerUI.OnClose  += OnPromptClose;
        }

        if (dayManager != null)
        {
            dayManager.OnCustomerSpawnTick += OnSpawnTick;
            dayManager.OnClosingTimeReached += OnClosingTime;
            dayManager.OnDayEnded += OnDayEnded;
        }
        else
        {
            // Fallback: no day system — spawn immediately (legacy behaviour)
            SpawnCustomer();
        }
    }

    private void OnDestroy()
    {
        if (customerUI != null)
        {
            customerUI.OnAccept -= OnPromptAccept;
            customerUI.OnReject -= OnPromptReject;
            customerUI.OnClose  -= OnPromptClose;
        }

        if (dayManager != null)
        {
            dayManager.OnCustomerSpawnTick -= OnSpawnTick;
            dayManager.OnClosingTimeReached -= OnClosingTime;
            dayManager.OnDayEnded -= OnDayEnded;
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
        Keyboard kb = Keyboard.current;
        if (kb != null && kb[pickUpKey].wasPressedThisFrame)
        {
            _isHoldingOrder = true;
            Debug.Log("Order picked up (B pressed). isHoldingOrder = true");
        }
    }

    private void HandleClickInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (_activeCustomer == null) return;

        // Ignore clicks while the prompt is already showing
        if (customerUI != null && customerUI.IsPromptVisible) return;

        // Raycast to see if the player clicked the customer
        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
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

        Debug.Log("Order Failed: DELIVERY TIMER EXPIRED");
        if (reputationManager != null) reputationManager.ChangeReputation(-10);
        ShowTimeoutExit();
        EndOrder();
    }

    // ── Order prompt ────────────────────────────────────────────────────────

    private void ShowOrderPrompt()
    {
        RecipeData recipe;

        // Use the factory-assigned recipe if available, else fallback to random
        if (_activeCustomer.Instance != null)
        {
            recipe = _activeCustomer.Instance.Order;
        }
        else if (recipes != null && recipes.Length > 0)
        {
            recipe = recipes[Random.Range(0, recipes.Length)];
        }
        else
        {
            Debug.LogError("[OrderManager] No recipes available!", this);
            return;
        }

        _activeCustomer.ShowOrder(recipe);

        // Pause patience timer while player is deciding
        if (_activeCustomer.patienceTimer != null)
            _activeCustomer.patienceTimer.Pause();

        // Build the prompt text from dialogue phrases
        string promptText = recipe.recipeName;
        if (_activeCustomer.Instance != null)
        {
            var dialogue = DialogueBuilder.BuildOrderDialogue(_activeCustomer.Instance);
            promptText = dialogue.order;
            if (!string.IsNullOrEmpty(dialogue.modifier))
                promptText += "\n" + dialogue.modifier;
        }

        if (customerUI != null)
            customerUI.ShowPrompt(promptText);

        SetCameraLocked(true);

        Debug.Log($"Showing order prompt: {promptText}");
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

        if (reputationManager != null) reputationManager.ChangeReputation(-5);
        Debug.Log("Order rejected — customer leaves.");

        // Show negative exit on reject
        if (_activeCustomer != null && _activeCustomer.Instance != null && customerUI != null)
        {
            string exit = _activeCustomer.Instance.Mood.GetNegativeExit();
            customerUI.ShowExitLine(exit);
        }
        else
        {
            HideUI();
        }

        if (_activeCustomer != null)
        {
            Destroy(_activeCustomer.gameObject);
            _activeCustomer = null;
        }

        // Notify day manager and only spawn next if still open
        if (dayManager != null)
        {
            dayManager.SetActiveCustomer(false);
            if (!dayManager.IsOpenForCustomers) return;
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

        HideUI();
        Debug.Log("Order prompt closed — customer still waiting.");
    }

    private void OnPatienceExpired()
    {
        Debug.Log("Customer lost patience and left! -7 Reputation");
        if (reputationManager != null) reputationManager.ChangeReputation(-7);

        // If the prompt is open, close it
        if (customerUI != null && customerUI.IsPromptVisible)
            customerUI.HidePrompt();

        SetCameraLocked(false);

        // Show timeout exit line
        ShowTimeoutExit();

        if (_activeCustomer != null)
        {
            Destroy(_activeCustomer.gameObject);
            _activeCustomer = null;
        }

        // Notify day manager and only spawn next if still open
        if (dayManager != null)
        {
            dayManager.SetActiveCustomer(false);
            if (!dayManager.IsOpenForCustomers) return;
        }

        Invoke(nameof(SpawnCustomer), 1.5f);
    }

    private void SetCameraLocked(bool locked)
    {
        if (playerCamera != null)
            playerCamera.InputEnabled = !locked;
    }

    private void HideUI()
    {
        if (customerUI != null)
            customerUI.Hide();
    }

    // ── Dialogue helpers ─────────────────────────────────────────────────────

    private void ShowTimeoutExit()
    {
        if (customerUI == null || _activeCustomer == null || _activeCustomer.Instance == null)
            return;

        string exit = DialogueBuilder.GetTimeoutExitLine(_activeCustomer.Instance);
        customerUI.ShowExitLine(exit);
    }

    private void ShowResultExit(OrderResult result)
    {
        if (customerUI == null || _activeCustomer == null || _activeCustomer.Instance == null)
            return;

        string exit = DialogueBuilder.GetExitLine(_activeCustomer.Instance, result);
        customerUI.ShowExitLine(exit);
    }

    // ── Order flow ───────────────────────────────────────────────────────────

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
            if (moneyManager != null)
                moneyManager.AddIncome(recipe.price);
            ShowResultExit(OrderResult.Perfect);
            _isHoldingOrder = false;
            EndOrder();
            return;
        }

        // Get the modifier (if any) for validation
        OrderModifier modifier = _activeCustomer.Instance?.Modifier;

        // Validate the held item against the recipe + modifier
        OrderResult result = OrderValidator.Validate(
            recipe,
            modifier,
            heldItem.preparedIngredients,
            heldItem.cookState
        );

        switch (result)
        {
            case OrderResult.Perfect:
                Debug.Log($"Order Delivered: PERFECT! ({recipe.recipeName})");
                if (reputationManager != null) reputationManager.ChangeReputation(+5);
                if (moneyManager != null) moneyManager.AddIncome(recipe.price);
                break;

            case OrderResult.WrongIngredients:
                Debug.Log($"Order Failed: WRONG INGREDIENTS for {recipe.recipeName}");
                if (reputationManager != null) reputationManager.ChangeReputation(-10);
                break;

            case OrderResult.Raw:
                Debug.Log($"Order Failed: RAW — {recipe.recipeName} needs toasting!");
                if (reputationManager != null) reputationManager.ChangeReputation(-10);
                break;

            case OrderResult.Burnt:
                Debug.Log($"Order Failed: BURNT — {recipe.recipeName} was overcooked!");
                if (reputationManager != null) reputationManager.ChangeReputation(-10);
                break;
        }

        ShowResultExit(result);

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

        // Notify day manager that the customer slot is free
        if (dayManager != null)
        {
            dayManager.SetActiveCustomer(false);

            // Don't spawn new customers if the shop is no longer open
            if (!dayManager.IsOpenForCustomers) return;
        }

        // Spawn the next customer after a short delay (legacy path or while open)
        Invoke(nameof(SpawnCustomer), 1.5f);
    }

    // ── Day system callbacks ────────────────────────────────────────────────

    /// <summary>Called by DayManager on each spawn-interval tick while the shop is open.</summary>
    private void OnSpawnTick()
    {
        // Only spawn if there is no customer already present
        if (_activeCustomer != null) return;
        SpawnCustomer();
    }

    /// <summary>Called when closing time is reached — stop spawning after the current customer.</summary>
    private void OnClosingTime()
    {
        Debug.Log("[OrderManager] Closing time — no more customers after the current one.");
        // If there is no active customer, tell DayManager immediately
        if (_activeCustomer == null && dayManager != null)
            dayManager.SetActiveCustomer(false);
    }

    /// <summary>Called when the player ends the day. Clean up for next day.</summary>
    private void OnDayEnded(int dayNumber)
    {
        // Cancel any pending spawns from legacy Invoke calls
        CancelInvoke(nameof(SpawnCustomer));

        if (_activeCustomer != null)
        {
            Destroy(_activeCustomer.gameObject);
            _activeCustomer = null;
        }

        _orderActive = false;
        _isHoldingOrder = false;

        if (timerUI != null) timerUI.StopTimer();
        HideUI();
    }

    // ── Spawning ─────────────────────────────────────────────────────────────

    private void SpawnCustomer()
    {
        // Don't spawn new customers after game over
        if (reputationManager != null && reputationManager.IsGameOver) return;

        // Don't spawn if the day system says we're closed
        if (dayManager != null && !dayManager.IsOpenForCustomers) return;

        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[OrderManager] customerPrefab or spawnPoint is not assigned!", this);
            return;
        }

        _activeCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        _isHoldingOrder = false;

        // Notify day manager that we have an active customer
        if (dayManager != null)
            dayManager.SetActiveCustomer(true);

        // Initialise with factory-created instance data
        if (customerFactory != null)
        {
            CustomerInstance instance = customerFactory.CreateNormalCustomer();
            if (instance != null)
                _activeCustomer.Initialise(instance);
        }

        // Show opening line on spawn
        if (_activeCustomer.Instance != null && customerUI != null)
        {
            string opening = DialogueBuilder.GetOpeningLine(_activeCustomer.Instance);
            customerUI.ShowOpeningLine(opening);
        }

        // Start the patience timer (adjusted by archetype multiplier)
        if (_activeCustomer.patienceTimer != null)
        {
            float patience = _activeCustomer.Instance != null
                ? _activeCustomer.Instance.GetPatienceDuration(patienceDuration)
                : patienceDuration;

            _activeCustomer.patienceTimer.OnExpired += OnPatienceExpired;
            _activeCustomer.patienceTimer.StartTimer(patience);
        }

        string archetypeName = _activeCustomer.Instance != null
            ? _activeCustomer.Instance.Archetype.archetypeName
            : "Generic";
        string moodName = _activeCustomer.Instance?.Mood != null
            ? _activeCustomer.Instance.Mood.moodName
            : "default";

        Debug.Log($"New customer spawned: {archetypeName} ({moodName})");
    }
}
