using System.Collections;
using System.Collections.Generic;
using Bufe.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bufe
{
    public class OrderManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("Prefab with Collider, Animator, and Customer component.")]
        [SerializeField] private Customer customerPrefab;

        [Header("Waypoints")]
        [Tooltip("World-space position where new customers appear.")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Position in front of the counter where the customer stands and waves. " +
                 "The transform's forward direction is the facing direction.")]
        [SerializeField] private Transform counterPoint;

        [Tooltip("Position the customer walks to after the order is done. " +
                 "Self-destroys on arrival.")]
        [SerializeField] private Transform exitPoint;

        [Header("UI & Systems")]
        [Tooltip("Player's hand controller — used to read the item in hand for validation.")]
        [SerializeField] private PlayerHand playerHand;

        [Tooltip("Unified customer UI — dialogue bubbles + accept/reject/close prompt.")]
        [SerializeField] private CustomerOrderUI customerUI;

        [Tooltip("Player camera — rotation is disabled while the prompt is visible.")]
        [SerializeField] private PlayerCameraRotation playerCamera;

        [Tooltip("Player look controller — UIMode is set while the prompt is visible " +
                 "so the cursor stays free and the auto-relock is suppressed.")]
        [SerializeField] private PlayerLook playerLook;

        [Tooltip("Factory that creates customer instances from archetype pool.")]
        [SerializeField] private CustomerFactory customerFactory;

        [Tooltip("Day manager — controls when customers can spawn.")]
        [SerializeField] private DayManager dayManager;

        [Tooltip("Money tracker — receives income from delivered orders.")]
        [SerializeField] private MoneyManager moneyManager;

        [Header("Tutorial")]
        [Tooltip("Day-1 scripted tutorial visit. Leave empty to disable the tutorial.")]
        [SerializeField] private TutorialData tutorialData;

        [Header("Order Ticket")]
        [Tooltip("Paper note prefab spawned on the counter to show the active order. Needs an OrderTicket component.")]
        [SerializeField] private OrderTicket ticketPrefab;

        [Tooltip("Where the order ticket is placed on the counter.")]
        [SerializeField] private Transform ticketSpawnPoint;

        [Header("Settings")]
        [Tooltip("Fallback order duration if GameManager/GameConfig is not present.")]
        [SerializeField] private float orderDuration = 30f;

        [Tooltip("Fallback delay if GameManager/GameConfig is not present.")]
        [SerializeField] private float timeBetweenCustomers = 3f;

        [Header("Faction Members (scripted spawns)")]
        [Tooltip("Scripted faction-member visits.")]
        [SerializeField] private List<FactionSpawnEntry> factionSpawns = new List<FactionSpawnEntry>();

        [Tooltip("Seconds the faction relationship bar lingers after a choice before the member leaves.")]
        [SerializeField] private float factionReactionDuration = 1.2f;

        [Tooltip("Where granted items spawn on the counter for the player to pick up. " +
                 "Falls back to the order ticket point if unset.")]
        [SerializeField] private Transform itemGrantPoint;

        [System.Serializable]
        public class FactionSpawnEntry
        {
            [Tooltip("In-game day this faction member appears on.")]
            public int day = 1;

            [Tooltip("Normal customers served before this member appears (0 = first customer). " +
                     "If the day ends before reaching this count, the member is force-spawned as the last customer.")]
            [Min(0)] public int spawnAfterCustomers = 0;

            [Tooltip("The interaction this member presents.")]
            public FactionInteraction interaction;

            // Runtime only — resets each Play session (booth resets via scene reload).
            [System.NonSerialized] public bool spawned;
        }

        #endregion

        #region Private Variables

        private Customer    _activeCustomer;
        private bool        _orderActive;
        private bool        _spawnScheduled;
        private bool        _spawningPaused;
        private OrderTicket _activeTicket;
        private int         _normalCustomersToday;

        // Tutorial runtime (no save system — resets on scene reload).
        private bool _tutorialDone;
        private bool _tutorialConversationStarted;
        private bool _tutorialConversationDone;
        private int  _tutorialLineIndex;

        // Day-büfe events scheduled as follow-ups (from any event's choice), due today.
        private readonly List<FactionInteraction> _followUpDaySpawns = new List<FactionInteraction>();

        #endregion        

        /// <summary>When true, no new customers will spawn until resumed.</summary>
        public bool SpawningPaused => _spawningPaused;
        
        /// <summary>
        /// Called by GameManager.ApplyConfig() to override settings from GameConfig.
        /// </summary>
        public void Init(float configOrderDuration, float configTimeBetween)
        {
            orderDuration        = configOrderDuration;
            timeBetweenCustomers = configTimeBetween;
        }

        #region Unity Methods

        private void Start()
        {
            if (customerUI != null)
            {
                customerUI.OnAccept += OnPromptAccept;
                customerUI.OnReject += OnPromptReject;
                customerUI.OnClose  += OnPromptClose;
                customerUI.OnChoicePicked += OnFactionChoicePicked;
                customerUI.OnContinue += OnTutorialContinue;
            }

            if (dayManager != null)
            {
                dayManager.OnDayStarted += OnDayStarted;
                dayManager.OnCustomerSpawnTick += OnSpawnTick;
                dayManager.OnClosingTimeReached += OnClosingTime;
                dayManager.OnDayEnded += OnDayEnded;

                // Freeze the day for the tutorial visit until its order is delivered.
                if (tutorialData != null && !_tutorialDone)
                    dayManager.HasPendingTutorial = true;
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
                customerUI.OnChoicePicked -= OnFactionChoicePicked;
                customerUI.OnContinue -= OnTutorialContinue;
            }

            if (dayManager != null)
            {
                dayManager.OnDayStarted -= OnDayStarted;
                dayManager.OnCustomerSpawnTick -= OnSpawnTick;
                dayManager.OnClosingTimeReached -= OnClosingTime;
                dayManager.OnDayEnded -= OnDayEnded;
            }
        }

        private void Update()
        {
            HandleClickInput();
        }

        #endregion

        #region Input Handling

        private void HandleClickInput()
        {
            // Only process while the cursor is locked (gameplay mode, not UI mode)
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (_activeCustomer == null) return;

            // Ignore clicks while the prompt is already showing
            if (customerUI != null && customerUI.IsPromptVisible) return;

            // Raycast from the viewport center — aligned with the crosshair
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            Customer clicked = hit.collider.GetComponent<Customer>()
                               ?? hit.collider.GetComponentInParent<Customer>();

            if (clicked == null || clicked != _activeCustomer) return;

            switch (_activeCustomer.CurrentState)
            {
                case Customer.State.WaitingForOrder:
                    // Tutorial dayı: the first click starts the conversation. After
                    // that the cursor is unlocked and the on-panel button drives it,
                    // so no further crosshair clicks reach here.
                    if (_activeCustomer.IsTutorial)
                    {
                        if (!_tutorialConversationStarted)
                            StartTutorialConversation();
                        break;
                    }

                    if (_activeCustomer.IsFactionMember)
                        HandleFactionClick();
                    else
                        ShowOrderPrompt();
                    break;

                case Customer.State.WaitingForFood:
                    TryDeliver();
                    break;
            }
        }

        #endregion
        
        #region Order Prompt

        private void ShowOrderPrompt()
        {
            Order recipe;

            // Use the factory-assigned recipe if available, else fallback to random
            if (_activeCustomer.Instance != null)
            {
                recipe = _activeCustomer.Instance.Order;
            }
            else
            {
                Debug.LogError("[OrderManager] No recipes available!", this);
                return;
            }

            _activeCustomer.ShowOrder(recipe);

            // Build the prompt text from dialogue phrases
            var dialogue = DialogueBuilder.BuildOrderDialogue(_activeCustomer.Instance);
            string promptText = dialogue.order;

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

            ShowOrderTicket(_activeCustomer.Recipe);

            Debug.Log($"Order accepted: TODO");
        }

        private void OnPromptReject()
        {
            SetCameraLocked(false);

            Debug.Log("Order rejected — customer leaves.");

            // Show negative exit on reject
            // todo(Ahmetcan): Change this later
            if (_activeCustomer != null && _activeCustomer.Instance != null && customerUI != null)
            {
                string exit = _activeCustomer.Instance.Mood.GetExitLine();
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

        // ── Faction interaction (scripted member at the counter) ─────────────

        private void HandleFactionClick()
        {
            FactionInteraction interaction = _activeCustomer.FactionInteraction;
            if (interaction == null) return;

            if (interaction.kind == InteractionKind.Collect)
                TryCollect(interaction);
            else
                ShowFactionPrompt(interaction);
        }

        private void ShowFactionPrompt(FactionInteraction interaction)
        {
            if (customerUI == null) return;

            _activeCustomer.ShowFactionInteraction();
            customerUI.ShowChoicePrompt(interaction.dialogue, interaction.choices, interaction.faction);
            SetCameraLocked(true);

            Debug.Log($"[OrderManager] Faction offer: {interaction.memberName} ({interaction.faction?.displayName}).");
        }

        private void OnFactionChoicePicked(int index)
        {
            if (_activeCustomer == null || !_activeCustomer.IsFactionMember) return;

            FactionInteraction interaction = _activeCustomer.FactionInteraction;
            if (interaction == null || index < 0 || index >= interaction.choices.Count) return;

            EventChoice choice = interaction.choices[index];
            ApplyEffects(choice.effects);

            // Schedule the consequence (e.g. a night event a few days later).
            if (choice.followUpEvent != null)
                EventScreenManager.Instance?.ScheduleEvent(choice.followUpEvent, choice.followUpDelay);

            SetCameraLocked(false);

            // Let the inline relationship bar animate before the member walks off.
            StartCoroutine(FinishFactionInteraction());
        }

        /// <summary>
        /// Collect interaction: if the player is holding the required item, hand it over
        /// (consumed like an order delivery) and apply the collect effects. Otherwise show the
        /// reminder line — the member keeps waiting so the player can fetch the item.
        /// </summary>
        private void TryCollect(FactionInteraction interaction)
        {
            InteractableItem held = playerHand != null ? playerHand.HeldItem : null;

            bool match = held != null && interaction.requiredItem != null &&
                         OrderValidator.ValidateSlot(new OrderSlot { target = interaction.requiredItem }, held);

            if (match)
            {
                playerHand.DestroyHeldItem();
                ApplyEffects(interaction.onCollectedEffects);

                Debug.Log($"[OrderManager] Collected '{interaction.requiredItem.itemName}' for {interaction.faction?.displayName}.");
                StartCoroutine(FinishFactionInteraction());
            }
            else if (customerUI != null)
            {
                // Not holding the item yet — remind and keep waiting.
                customerUI.ShowLine(interaction.dialogue);
            }
        }

        private IEnumerator FinishFactionInteraction()
        {
            yield return new WaitForSeconds(factionReactionDuration);
            HideUI();
            SendCustomerAway();
        }

        /// <summary>Applies authored effects via the shared resolver, granting items at the counter.</summary>
        private void ApplyEffects(List<EventEffect> effects)
        {
            Transform grantPoint = itemGrantPoint != null ? itemGrantPoint : ticketSpawnPoint;
            EffectResolver.Apply(effects, moneyManager, RelationshipManager.Instance,
                                 FactionManager.Instance, grantPoint, "Faction");
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

        private void ShowOrderTicket(Order order)
        {
            if (ticketPrefab == null || ticketSpawnPoint == null) return;

            HideOrderTicket();
            _activeTicket = Instantiate(ticketPrefab, ticketSpawnPoint);
            _activeTicket.SetOrder(order);
        }

        private void HideOrderTicket()
        {
            if (_activeTicket == null) return;

            Destroy(_activeTicket.gameObject);
            _activeTicket = null;
        }

        #endregion
        
        #region Order Flow

        private void TryDeliver()
        {
            InteractableItem heldItem = playerHand != null ? playerHand.HeldItem : null;
            if (heldItem == null)
            {
                Debug.Log("[OrderManager] Cannot deliver — not holding anything!");
                return;
            }

            Order order = _activeCustomer?.Recipe;
            if (order == null) return;
            
            // Find the first unfulfilled slot that matches the held item
            OrderSlot matched = null;
            foreach (OrderSlot slot in order.slots)
            {
                if (!slot.isFulfilled && OrderValidator.ValidateSlot(slot, heldItem))
                {
                    matched = slot;
                    break;
                }
            }

            if (matched == null)
            {
                Debug.Log($"[OrderManager] Wrong item: {heldItem.itemName}");
                return;
            }

            matched.isFulfilled = true;
            _activeTicket?.SetOrder(order);
            playerHand.DestroyHeldItem();
            Debug.Log($"[OrderManager] Slot fulfilled: {matched.target.itemName}");

            if (order.IsComplete)
            {
                Debug.Log("[OrderManager] Order complete!");
                bool wasTutorial = _activeCustomer != null && _activeCustomer.IsTutorial;

                if (moneyManager != null) moneyManager.AddIncome(order.TotalPrice);

                if (wasTutorial && tutorialData != null && customerUI != null)
                    customerUI.ShowExitLine(tutorialData.thanksLine);
                else
                    ShowResultExit();

                EndOrder();

                if (wasTutorial)
                {
                    _tutorialDone = true;
                    dayManager?.BeginTimedPhase();   // unfreeze: clock + spawns start now
                }
            }
        }

        private void EndOrder()
        {
            _orderActive = false;

            SendCustomerAway();
        }

        private void SendCustomerAway()
        {
            HideOrderTicket();

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
                // Shop closed: still honor any scripted faction member that hasn't appeared yet.
                if (!dayManager.IsOpenForCustomers)
                {
                    FactionInteraction pending = GetPendingFactionInteractionToday();
                    if (pending != null)
                    {
                        SpawnFactionMember(pending);   // re-occupies the customer slot
                        return;
                    }

                    dayManager.SetActiveCustomer(false);
                    return;
                }

                dayManager.SetActiveCustomer(false);
            }

            // Schedule the next customer spawn (guard against double-spawn)
            if (!_spawnScheduled)
            {
                _spawnScheduled = true;
                Invoke(nameof(SpawnCustomer), timeBetweenCustomers);
            }
        }
        
        private void ShowResultExit()
        {
            if (customerUI == null || _activeCustomer == null || _activeCustomer.Instance == null)
                return;

            string exit = DialogueBuilder.GetExitLine(_activeCustomer.Instance);
            customerUI.ShowExitLine(exit);
        }

        #endregion

        #region Day System Controls

        /// <summary>Called when the shutter opens. Pull any day-büfe events scheduled for today.</summary>
        private void OnDayStarted()
        {
            if (EventScreenManager.Instance != null && dayManager != null)
                _followUpDaySpawns.AddRange(EventScreenManager.Instance.ConsumeDueDayEvents(dayManager.CurrentDay));

            // Day-1 tutorial: the scripted dayı arrives first, before any random
            // customer and while the clock is frozen.
            if (tutorialData != null && !_tutorialDone && _activeCustomer == null)
                SpawnTutorialCustomer();
        }

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

            if (_activeCustomer != null || dayManager == null) return;

            // Honor any scripted faction member that hasn't appeared yet today.
            FactionInteraction pending = GetPendingFactionInteractionToday();
            if (pending != null)
                SpawnFactionMember(pending);
            else
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

            _activeCustomer       = null;
            _orderActive          = false;
            _spawnScheduled       = false;
            _normalCustomersToday = 0;
            _followUpDaySpawns.Clear();
            HideOrderTicket();

            HideUI();
        }

        #endregion
        
        #region Customer Spawner
        
        private void SpawnCustomer()
        {
            _spawnScheduled = false;

            // Don't spawn if the game is paused or over
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // Don't spawn if debug-paused
            if (_spawningPaused) return;

            // Don't spawn if the day system says we're closed
            if (dayManager != null && !dayManager.IsOpenForCustomers) return;

            if (customerPrefab == null || spawnPoint == null)
            {
                Debug.LogError("[OrderManager] customerPrefab or spawnPoint is not assigned!", this);
                return;
            }

            // Scripted faction member takes priority as today's spawn.
            FactionInteraction dueInteraction = GetDueFactionInteraction();
            if (dueInteraction != null)
            {
                SpawnFactionMember(dueInteraction);
                return;
            }

            _activeCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
            _normalCustomersToday++;

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

        /// <summary>
        /// Returns the scripted faction member due now (right day, not yet spawned, and enough
        /// normal customers have already been served), or null.
        /// </summary>
        private FactionSpawnEntry GetDueFactionSpawn()
        {
            if (dayManager == null) return null;

            int day = dayManager.CurrentDay;
            foreach (FactionSpawnEntry entry in factionSpawns)
            {
                if (entry != null && !entry.spawned && entry.interaction != null
                    && entry.day == day && _normalCustomersToday >= entry.spawnAfterCustomers)
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// Returns any scripted faction member for today that hasn't spawned yet, ignoring the
        /// customer-count gate. Used to force the member to appear before the day ends.
        /// </summary>
        private FactionSpawnEntry GetPendingFactionSpawnToday()
        {
            if (dayManager == null) return null;

            int day = dayManager.CurrentDay;
            foreach (FactionSpawnEntry entry in factionSpawns)
            {
                if (entry != null && !entry.spawned && entry.interaction != null && entry.day == day)
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// The day-büfe interaction due now: a count-gated seed entry, else a follow-up day
        /// event scheduled for today. Returns null if none is ready.
        /// </summary>
        private FactionInteraction GetDueFactionInteraction()
        {
            FactionSpawnEntry seed = GetDueFactionSpawn();
            if (seed != null) { seed.spawned = true; return seed.interaction; }

            if (_followUpDaySpawns.Count > 0)
            {
                FactionInteraction fi = _followUpDaySpawns[0];
                _followUpDaySpawns.RemoveAt(0);
                return fi;
            }
            return null;
        }

        /// <summary>
        /// Any day-büfe interaction still owed today (seed or follow-up), ignoring the
        /// customer-count gate. Used to force it to appear before the day ends.
        /// </summary>
        private FactionInteraction GetPendingFactionInteractionToday()
        {
            FactionSpawnEntry seed = GetPendingFactionSpawnToday();
            if (seed != null) { seed.spawned = true; return seed.interaction; }

            if (_followUpDaySpawns.Count > 0)
            {
                FactionInteraction fi = _followUpDaySpawns[0];
                _followUpDaySpawns.RemoveAt(0);
                return fi;
            }
            return null;
        }

        /// <summary>Spawns a faction member: walks to the counter and waits until clicked.</summary>
        private void SpawnFactionMember(FactionInteraction interaction)
        {
            _activeCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
            _activeCustomer.InitialiseFaction(interaction);

            if (dayManager != null)
                dayManager.SetActiveCustomer(true);

            // Collect members announce what they want on arrival; offer members wait silently.
            System.Action onArrived = null;
            if (interaction.kind == InteractionKind.Collect)
                onArrived = () => { if (customerUI != null) customerUI.ShowLine(interaction.dialogue); };

            if (counterPoint != null)
                _activeCustomer.StartArriving(counterPoint.position, counterPoint.rotation, onArrived);

            Debug.Log($"[OrderManager] Faction member spawned: {interaction.memberName} ({interaction.kind}).");
        }

        #endregion

        #region Tutorial

        /// <summary>Spawns the scripted day-1 tutorial visitor using the normal customer pipeline.</summary>
        private void SpawnTutorialCustomer()
        {
            if (customerPrefab == null || spawnPoint == null)
            {
                Debug.LogError("[OrderManager] customerPrefab or spawnPoint is not assigned!", this);
                return;
            }

            _activeCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
            _activeCustomer.InitialiseTutorial(BuildTutorialOrder());

            if (dayManager != null)
                dayManager.SetActiveCustomer(true);

            _tutorialConversationStarted = false;
            _tutorialConversationDone    = false;
            _tutorialLineIndex           = 0;

            // Walk to the counter and wait (waving). The conversation only starts
            // once the player clicks the dayı — see HandleClickInput.
            if (counterPoint != null)
                _activeCustomer.StartArriving(counterPoint.position, counterPoint.rotation);

            Debug.Log("[OrderManager] Tutorial dayı spawned.");
        }

        private Order BuildTutorialOrder()
        {
            var order = new Order { slots = new List<OrderSlot>() };
            if (tutorialData.orderItems != null)
            {
                foreach (OrderItem item in tutorialData.orderItems)
                    if (item != null)
                        order.slots.Add(new OrderSlot { target = item });
            }
            return order;
        }

        /// <summary>
        /// Begins the conversation once the dayı reaches the counter. The order panel
        /// stays open the whole time; the cursor is unlocked so the panel button is
        /// clickable.
        /// </summary>
        private void StartTutorialConversation()
        {
            _tutorialConversationStarted = true;
            _tutorialLineIndex           = 0;
            SetCameraLocked(true);   // free the cursor for the panel button
            ShowTutorialStep();
        }

        /// <summary>Shows the current intro line on the panel with its advance button.</summary>
        private void ShowTutorialStep()
        {
            var lines = tutorialData.introLines;

            if (lines == null || lines.Count == 0 || _tutorialLineIndex >= lines.Count)
            {
                _tutorialConversationDone = true;
                ShowTutorialPrompt();
                return;
            }

            TutorialData.TutorialLine line = lines[_tutorialLineIndex];
            if (customerUI != null)
                customerUI.ShowContinue(line.text, line.buttonLabel);
        }

        /// <summary>Panel button clicked during the tutorial conversation — advance to the next line.</summary>
        private void OnTutorialContinue()
        {
            if (_activeCustomer == null || !_activeCustomer.IsTutorial || _tutorialConversationDone) return;

            _tutorialLineIndex++;
            ShowTutorialStep();
        }

        /// <summary>Presents the fixed tutorial order with a forced Accept (no reject/close).</summary>
        private void ShowTutorialPrompt()
        {
            if (_activeCustomer == null) return;

            _activeCustomer.ShowOrder(_activeCustomer.Recipe);

            if (customerUI != null)
                customerUI.ShowPrompt(tutorialData.orderPromptLine,
                                      showReject: false, showClose: false,
                                      acceptLabel: tutorialData.orderButtonLabel);
        }

        #endregion

        #region Helper Methods

        /// <summary>Toggle customer spawning on/off (debug).</summary>
        public void SetSpawningPaused(bool paused)
        {
            _spawningPaused = paused;
            Debug.Log($"[OrderManager] Customer spawning {(paused ? "PAUSED" : "RESUMED")}.");
        }

        #endregion
    }
}
