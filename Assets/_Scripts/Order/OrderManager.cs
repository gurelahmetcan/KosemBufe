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

        SpawnCustomer();
    }

    private void OnDestroy()
    {
        if (customerUI != null)
        {
            customerUI.OnAccept -= OnPromptAccept;
            customerUI.OnReject -= OnPromptReject;
            customerUI.OnClose  -= OnPromptClose;
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

        // Spawn the next customer after a short delay
        Invoke(nameof(SpawnCustomer), 1.5f);
    }

    // ── Spawning ─────────────────────────────────────────────────────────────

    private void SpawnCustomer()
    {
        // Don't spawn new customers after game over
        if (reputationManager != null && reputationManager.IsGameOver) return;

        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[OrderManager] customerPrefab or spawnPoint is not assigned!", this);
            return;
        }

        _activeCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        _isHoldingOrder = false;

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
