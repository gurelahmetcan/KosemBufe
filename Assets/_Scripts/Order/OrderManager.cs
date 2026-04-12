using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Orchestrates the full order-delivery loop for normal customers:
///
///   1. CustomerFactory creates a CustomerInstance (archetype + mood + order + modifier).
///   2. Customer spawns at spawnPoint → walks to counterPoint (Walking anim).
///   3. Customer arrives → waves (Wave anim) + opening line shown.
///   4. Player clicks customer → order line (+ optional modifier) shown, prompt appears.
///   5. Player accepts → customer idles (Idle anim), timer starts.
///   6. Player clicks customer with prepared item → validate (recipe + modifier).
///   7. Result → exit line shown, customer walks to exitPoint (Walking anim) → self-destroys.
///   8. Timer expires → timeout exit line shown, customer leaves.
///
/// Spawning is driven by DayManager: customers only arrive while the shop is open.
/// </summary>
public class OrderManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab with Collider, Animator, and Customer component.")]
    public Customer customerPrefab;

    [Header("Waypoints")]
    [Tooltip("World-space position where new customers appear.")]
    public Transform spawnPoint;

    [Tooltip("Position in front of the counter where the customer stands and waves. " +
             "The transform's forward direction is the facing direction.")]
    public Transform counterPoint;

    [Tooltip("Position the customer walks to after the order is done. " +
             "Self-destroys on arrival.")]
    public Transform exitPoint;

    [Header("UI & Systems")]
    [FormerlySerializedAs("slicingController")]
    [Tooltip("Player's InHandController — used to read the item in hand for validation.")]
    public InHandController ınHandController;

    [Tooltip("Unified customer UI — dialogue bubbles + accept/reject/close prompt.")]
    public CustomerOrderUI customerUI;

    [Tooltip("Player camera — rotation is disabled while the prompt is visible.")]
    public PlayerCameraRotation playerCamera;

    [Tooltip("Player look controller — UIMode is set while the prompt is visible " +
             "so the cursor stays free and the auto-relock is suppressed.")]
    public PlayerLook playerLook;

    [Tooltip("Reputation tracker — receives score changes from order outcomes.")]
    public ReputationManager reputationManager;

    [Tooltip("Factory that creates customer instances from archetype pool.")]
    public CustomerFactory customerFactory;

    [Tooltip("Day manager — controls when customers can spawn.")]
    public DayManager dayManager;

    [Tooltip("Money tracker — receives income from delivered orders.")]
    public MoneyManager moneyManager;

    [Header("Settings")]
    [Tooltip("Fallback order duration if GameManager/GameConfig is not present.")]
    public float orderDuration = 30f;

    [Tooltip("Fallback delay if GameManager/GameConfig is not present.")]
    public float timeBetweenCustomers = 3f;

    [Tooltip("Fallback recipes — used only if CustomerFactory is not assigned.")]
    public RecipeData[] recipes;

    [Header("Input")]
    [Tooltip("Key to simulate picking up the prepared item (dev shortcut).")]
    public Key pickUpKey = Key.B;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private Customer _activeCustomer;
    private bool     _isHoldingOrder;
    private bool     _orderActive;
    private bool     _spawnScheduled;
    private bool     _spawningPaused;

    /// <summary>When true, no new customers will spawn until resumed.</summary>
    public bool SpawningPaused => _spawningPaused;

    /// <summary>Toggle customer spawning on/off (debug).</summary>
    public void SetSpawningPaused(bool paused)
    {
        _spawningPaused = paused;
        Debug.Log($"[OrderManager] Customer spawning {(paused ? "PAUSED" : "RESUMED")}.");
    }

    // ── Init (called by GameManager) ─────────────────────────────────────────

    /// <summary>
    /// Called by GameManager.ApplyConfig() to override settings from GameConfig.
    /// </summary>
    public void Init(float configOrderDuration, float configTimeBetween)
    {
        orderDuration        = configOrderDuration;
        timeBetweenCustomers = configTimeBetween;
    }

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
        // Only process while the cursor is locked (gameplay mode, not UI mode)
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (_activeCustomer == null) return;

        // Ignore clicks while the prompt is already showing
        if (customerUI != null && customerUI.IsPromptVisible) return;

        // Raycast from the viewport centre — aligned with the crosshair
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
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

        _activeCustomer.AcceptOrder();
        _orderActive = true;

        Debug.Log($"Order accepted: {_activeCustomer.Recipe.recipeName}");
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

        SendCustomerAway();
    }

    private void OnPromptClose()
    {
        SetCameraLocked(false);

        // Return customer to WaitingForOrder so the player can click again later
        if (_activeCustomer != null)
            _activeCustomer.ResetCustomer();

        HideUI();
        Debug.Log("Order prompt closed — customer still waiting.");
    }

    private void SetCameraLocked(bool locked)
    {
        // Legacy incremental rotation (A/D snap turns)
        if (playerCamera != null)
            playerCamera.InputEnabled = !locked;

        // UIMode suppresses PlayerLook's auto-relock so the same click that
        // opens the UI doesn't immediately re-lock the cursor again.
        // PlayerLook.Update() will force the cursor free while UIMode is true
        // and re-lock it as soon as UIMode is set back to false.
        if (playerLook != null)
            playerLook.UIMode = locked;

        // Immediately apply cursor state so other systems see it this frame.
        if (locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
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
                Debug.Log($"Order Failed: RAW — {recipe.recipeName} needs cooking!");
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

        SendCustomerAway();
    }

    /// <summary>
    /// Makes the active customer walk to the exit and frees the slot
    /// so the next customer can spawn after a delay.
    /// </summary>
    private void SendCustomerAway()
    {
        if (_activeCustomer != null)
        {
            if (exitPoint != null)
            {
                // Customer walks to exit and self-destroys on arrival
                _activeCustomer.StartLeaving(exitPoint.position);
            }
            else
            {
                Destroy(_activeCustomer.gameObject);
            }
            _activeCustomer = null;
        }

        // Notify day manager that the customer slot is free
        if (dayManager != null)
        {
            dayManager.SetActiveCustomer(false);

            // Don't spawn new customers if the shop is no longer open
            if (!dayManager.IsOpenForCustomers) return;
        }

        // Schedule the next customer spawn (guard against double-spawn)
        if (!_spawnScheduled)
        {
            _spawnScheduled = true;
            Invoke(nameof(SpawnCustomer), timeBetweenCustomers);
        }
    }

    // ── Day system callbacks ────────────────────────────────────────────────

    /// <summary>Called by DayManager on each spawn-interval tick while the shop is open.</summary>
    private void OnSpawnTick()
    {
        // Only spawn if there is no customer and no spawn already scheduled
        if (_activeCustomer != null || _spawnScheduled) return;
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

        // Destroy all customers (active + any still walking to exit)
        foreach (Customer c in FindObjectsByType<Customer>(FindObjectsSortMode.None))
            Destroy(c.gameObject);

        _activeCustomer = null;
        _orderActive    = false;
        _isHoldingOrder = false;
        _spawnScheduled = false;

        HideUI();
    }

    // ── Spawning ─────────────────────────────────────────────────────────────

    private void SpawnCustomer()
    {
        _spawnScheduled = false;

        // Don't spawn if the game is paused or over
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // Don't spawn if debug-paused
        if (_spawningPaused) return;

        // Don't spawn new customers after game over (fallback if no GameManager)
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

        // Walk to the counter — opening line shown on arrival
        if (counterPoint != null)
        {
            _activeCustomer.StartArriving(counterPoint.position, counterPoint.rotation, () =>
            {
                ShowOpeningLine();
            });
        }
        else
        {
            // No counter point — legacy fallback: appear directly and show opening line
            ShowOpeningLine();
        }

        string archetypeName = _activeCustomer.Instance != null
            ? _activeCustomer.Instance.Archetype.archetypeName
            : "Generic";
        string moodName = _activeCustomer.Instance?.Mood != null
            ? _activeCustomer.Instance.Mood.moodName
            : "default";

        Debug.Log($"New customer spawned: {archetypeName} ({moodName})");
    }

    private void ShowOpeningLine()
    {
        if (_activeCustomer == null) return;
        if (_activeCustomer.Instance == null || customerUI == null) return;

        string opening = DialogueBuilder.GetOpeningLine(_activeCustomer.Instance);
        customerUI.ShowOpeningLine(opening);
    }
}
