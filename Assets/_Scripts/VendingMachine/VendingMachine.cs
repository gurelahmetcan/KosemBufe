using System.Collections;
using UnityEngine;

/// <summary>
/// Core state machine for the Ingredient Vending Machine (IVM).
///
/// States:
///   IDLE          → All buttons active, waiting for input.
///   DISPENSING    → Spiral rotates, all buttons locked. Jam check at 80%.
///   JAMMED        → Spiral stuck, button flashes red, machine shakes. Player must kick.
///   READY_TO_PICK → Item in tray, buttons locked until player picks it up.
///
/// Setup:
///   1. Attach to the root vending machine GameObject.
///   2. Assign config, dispenseTray, all slots, and all buttons.
///   3. Each VendingMachineButton.slotIndex must correspond to the slots array index.
///   4. Assign cameraShake if screen shake on jam/kick is desired.
///   5. Assign optional AudioSource + clips for sound effects.
/// </summary>
public class VendingMachine : MonoBehaviour
{
    public enum State { Idle, Dispensing, Jammed, ReadyToPick }

    [Header("Configuration")]
    [Tooltip("ScriptableObject with tuning parameters.")]
    public VendingMachineConfig config;

    [Header("References")]
    [Tooltip("The dispenser tray trigger at the bottom of the machine.")]
    public DispenseTray dispenseTray;

    [Tooltip("Camera shake component for jam/kick feedback.")]
    public CameraShake cameraShake;

    [Header("Slots & Buttons")]
    [Tooltip("All ingredient slots. Index must match the button's slotIndex.")]
    public VendingMachineSlot[] slots;

    [Tooltip("All vending buttons. Each button references a slot by index.")]
    public VendingMachineButton[] buttons;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip dispenseSound;
    public AudioClip jamSound;
    public AudioClip kickSound;
    public AudioClip itemLandSound;

    [Header("Auto Pickup")]
    [Tooltip("SlicingController on the player — used to auto-deliver dispensed items to the player's hand.")]
    public SlicingController slicingController;

    [Header("Kick / Mash Settings")]
    [Tooltip("Key the player presses repeatedly to unjam the machine.")]
    public KeyCode kickKey = KeyCode.F;

    [Tooltip("Max distance from which the player can interact with the machine.")]
    public float kickRange = 3f;

    [Tooltip("Tag on the player GameObject for distance check. Leave empty to skip range check.")]
    public string playerTag = "Player";

    [Header("Mash Meter UI")]
    [Tooltip("UI controller for the floating 'F' prompt and progress bar.")]
    public MashMeterUI mashMeterUI;

    // ── Public state ────────────────────────────────────────────────────────────

    public State CurrentState { get; private set; } = State.Idle;

    // ── Runtime ─────────────────────────────────────────────────────────────────

    private int        _activeSlotIndex = -1;
    private GameObject _dispensedItem;
    private Coroutine  _dispenseRoutine;
    private float      _mashProgress;

    // ── Initialization ──────────────────────────────────────────────────────────

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("[VendingMachine] Config asset is not assigned!", this);
            return;
        }

        foreach (VendingMachineSlot slot in slots)
            slot.InitStock(config.maxStockPerSlot);

        if (dispenseTray != null)
            dispenseTray.OnItemLanded += OnItemLandedInTray;

        SetState(State.Idle);
    }

    private void OnDestroy()
    {
        if (dispenseTray != null)
            dispenseTray.OnItemLanded -= OnItemLandedInTray;
    }

    // ── Update ──────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentState == State.Jammed)
            HandleJammedInput();
    }

    // ── Public API (called by VendingMachineButton) ─────────────────────────────

    /// <summary>
    /// Called when a button is pressed. Only accepted in Idle state.
    /// Returns true if the dispense was started.
    /// </summary>
    public bool RequestDispense(int slotIndex)
    {
        if (CurrentState != State.Idle) return false;
        if (slotIndex < 0 || slotIndex >= slots.Length) return false;
        if (!slots[slotIndex].HasStock) return false;

        _activeSlotIndex = slotIndex;
        _dispenseRoutine = StartCoroutine(DispenseRoutine());
        return true;
    }

    /// <summary>
    /// Called when the mash meter reaches 100% — delivers the final kick.
    /// </summary>
    private void CompleteMashKick()
    {
        PlaySound(kickSound);

        // Camera shake on the final kick
        if (cameraShake != null)
            cameraShake.Shake();

        // Spawn and release the jammed item
        if (_dispensedItem != null)
        {
            Rigidbody rb = _dispensedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(Vector3.down * config.kickForce, ForceMode.Impulse);
            }
        }
        else
        {
            VendingMachineSlot slot = slots[_activeSlotIndex];
            _dispensedItem = slot.SpawnItem();

            if (_dispensedItem != null)
            {
                Rigidbody rb = _dispensedItem.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(Vector3.down * config.kickForce, ForceMode.Impulse);
            }
        }

        // Clear jam visuals
        VendingMachineButton activeBtn = FindButtonForSlot(_activeSlotIndex);
        if (activeBtn != null)
            activeBtn.ClearJamVisual();

        if (mashMeterUI != null)
            mashMeterUI.Hide();

        SetState(State.ReadyToPick);
    }

    // ── State transitions ───────────────────────────────────────────────────────

    private void SetState(State newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case State.Idle:
                _activeSlotIndex = -1;
                SetAllButtonsLocked(false);
                break;

            case State.Dispensing:
                SetAllButtonsLocked(true);
                break;

            case State.Jammed:
                SetAllButtonsLocked(true);
                _mashProgress = 0f;
                if (mashMeterUI != null)
                    mashMeterUI.Show();
                break;

            case State.ReadyToPick:
                SetAllButtonsLocked(true);
                break;
        }
    }

    // ── Dispense coroutine ──────────────────────────────────────────────────────

    private IEnumerator DispenseRoutine()
    {
        SetState(State.Dispensing);

        VendingMachineSlot slot = slots[_activeSlotIndex];
        float duration = config.dispenseDuration;
        float elapsed  = 0f;
        bool  jamChecked = false;

        PlaySound(dispenseSound);

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float normalized = dt / duration;

            slot.RotateSpiral(normalized);

            // Jam check at 80% completion
            if (!jamChecked && elapsed / duration >= 0.8f)
            {
                jamChecked = true;

                if (Random.value < config.jamProbability)
                {
                    // JAM triggered
                    PlaySound(jamSound);

                    if (cameraShake != null)
                        cameraShake.Shake();

                    VendingMachineButton activeBtn = FindButtonForSlot(_activeSlotIndex);
                    if (activeBtn != null)
                        activeBtn.ShowJamVisual();

                    SetState(State.Jammed);
                    yield break;
                }
            }

            yield return null;
        }

        // No jam — spawn the item and let it fall into the tray
        _dispensedItem = slot.SpawnItem();
        SetState(State.ReadyToPick);
    }

    // ── Jammed state — mash meter ────────────────────────────────────────────────

    private void HandleJammedInput()
    {
        // Range check — if a player tag is set, verify proximity
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                float dist = Vector3.Distance(player.transform.position, transform.position);
                if (dist > kickRange) return;
            }
        }

        // Decay when not pressing
        _mashProgress -= config.mashDecayRate * Time.deltaTime;
        _mashProgress  = Mathf.Max(0f, _mashProgress);

        // Each key press adds progress
        if (Input.GetKeyDown(kickKey))
        {
            _mashProgress += config.EffectiveMashPerPress;
            PlaySound(kickSound);

            // Small shake per press for feedback
            if (cameraShake != null)
                cameraShake.Shake();
        }

        _mashProgress = Mathf.Clamp01(_mashProgress);

        if (mashMeterUI != null)
            mashMeterUI.SetProgress(_mashProgress);

        // Mash complete — deliver the final kick
        if (_mashProgress >= 1f)
            CompleteMashKick();
    }

    // ── Auto pickup ─────────────────────────────────────────────────────────────

    private void OnItemLandedInTray(GameObject item)
    {
        // Only react to our own dispensed item
        if (CurrentState != State.ReadyToPick && CurrentState != State.Dispensing) return;
        if (item != _dispensedItem) return;

        PlaySound(itemLandSound);

        // Destroy the physics object — it's just the falling visual
        Destroy(_dispensedItem);
        _dispensedItem = null;
        dispenseTray.ResetCount();

        // Auto-deliver to the player's hand using the slot's HoldableItem
        if (slicingController != null && _activeSlotIndex >= 0)
        {
            HoldableItem holdable = slots[_activeSlotIndex].holdableItem;
            if (holdable != null)
                slicingController.PickUp(holdable);
        }

        StartCoroutine(CooldownThenIdle());
    }

    private IEnumerator CooldownThenIdle()
    {
        yield return new WaitForSeconds(config.itemCooldown);
        _dispensedItem = null;
        SetState(State.Idle);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private void SetAllButtonsLocked(bool locked)
    {
        foreach (VendingMachineButton btn in buttons)
            btn.SetLocked(locked);
    }

    private VendingMachineButton FindButtonForSlot(int slotIndex)
    {
        foreach (VendingMachineButton btn in buttons)
        {
            if (btn.slotIndex == slotIndex)
                return btn;
        }
        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
