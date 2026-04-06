using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Flat grill station.  The player clicks with a grillable item to place it
/// on the surface; after cookTimePerSide seconds the item needs flipping
/// (press F); after another cookTimePerSide seconds it becomes cooked.
/// Leaving it too long past either threshold causes it to burn.
///
/// Interaction (via IInteractable):
///   - Grillable item in hand → place on the next free slot.
///   - Empty hand (handled by GrillSlotDisplay on each item) → collect.
///
/// Scene Setup:
///   1. Place a GameObject with a Collider for the grill surface.
///   2. Add this component.
///   3. Create empty child Transforms for each slot position (up to capacity).
///   4. Assign slotPositions in the inspector.
///   5. On grillable item prefabs: set supportedProcesses = Grillable.
///   6. Optionally assign flipHintText for the "Press [F] to Flip" prompt.
/// </summary>
public class GrillStation : Station
{
    [Header("Slot Positions")]
    [Tooltip("Empty child Transforms marking where items sit on the grill. " +
             "Array length determines capacity (e.g. 4 slots).")]
    public Transform[] slotPositions;

    [Header("Timing")]
    [Tooltip("Seconds each side needs to cook before the next phase.")]
    public float cookTimePerSide = 5f;

    [Tooltip("Seconds of grace after a side is done before the item burns.")]
    public float burnGraceTime = 5f;

    [Header("Visual – Cook Colors")]
    [Tooltip("Tint applied to raw / just-placed items.")]
    public Color rawColor = new Color(0.85f, 0.35f, 0.35f);

    [Tooltip("Tint applied to fully-cooked items.")]
    public Color cookedColor = new Color(0.55f, 0.35f, 0.18f);

    [Tooltip("Tint applied to burnt items.")]
    public Color burntColor = new Color(0.15f, 0.10f, 0.05f);

    [Header("UI")]
    [Tooltip("Optional screen-space text for flip / collect hints.")]
    public TextMeshProUGUI hintText;

    // ── Runtime state ────────────────────────────────────────────────────────

    private GrillSlot[]  _slots;
    private GameObject[] _slotVisuals;

    public int Capacity => slotPositions != null ? slotPositions.Length : 0;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _slots       = new GrillSlot[Capacity];
        _slotVisuals = new GameObject[Capacity];
    }

    private void Update()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].Tick(Time.deltaTime);
                UpdateSlotVisual(i);
            }
        }

        UpdateHintUI();
    }

    // ── Station interface (placing items) ────────────────────────────────────

    public override bool CanInteract(InHandController hand)
    {
        if (hand.HeldItem == null) return false;
        if (!hand.HeldItem.supportedProcesses.HasFlag(ProcessType.Grillable)) return false;
        return FindFreeSlot() >= 0;
    }

    public override void Interact(InHandController hand)
    {
        int slot = FindFreeSlot();
        if (slot < 0) return;

        PlaceOnGrill(slot, hand);
    }

    // ── Public API (used by GrillSlotDisplay) ────────────────────────────────

    public GrillSlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return null;
        return _slots[index];
    }

    public void FlipSlot(int index)
    {
        GrillSlot slot = _slots[index];
        if (slot == null) return;

        if (slot.TryFlip())
            Debug.Log($"[Grill] Flipped slot {index}.");
    }

    public void CollectFromSlot(int index, InHandController hand)
    {
        GrillSlot slot = _slots[index];
        if (slot == null) return;

        // Determine cook state for the output item
        GrillState state = slot.GetState();
        CookState cookState;
        switch (state)
        {
            case GrillState.Cooked:  cookState = CookState.Cooked; break;
            case GrillState.Burnt:   cookState = CookState.Burnt;  break;
            default:                 cookState = CookState.Raw;    break;
        }

        // Create a standalone output item (not tied to any scene source object)
        GameObject outputObj = new GameObject($"Grilled_{slot.SavedIngredient}");
        HoldableItem output  = outputObj.AddComponent<HoldableItem>();
        output.itemName             = slot.SavedItemName;
        output.handPrefab           = slot.SavedHandPrefab;
        output.ingredient           = slot.SavedIngredient;
        output.supportedProcesses   = ProcessType.Grillable;
        output.cookState            = cookState;
        output.preparedIngredients  = new List<Ingredient> { slot.SavedIngredient };

        hand.PickUp(output);

        Debug.Log($"[Grill] Collected slot {index}: {slot.SavedItemName} ({cookState})");

        // Clean up
        ClearSlot(index);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void PlaceOnGrill(int index, InHandController hand)
    {
        HoldableItem item     = hand.HeldItem;
        bool         isSliced = hand.IsSliced;

        // Resolve the visual prefab (use sliced version when applicable)
        GameObject visualPrefab = item.handPrefab;
        if (isSliced)
        {
            SliceableItem sliceable = item.GetComponent<SliceableItem>();
            if (sliceable != null && sliceable.slicedHandPrefab != null)
                visualPrefab = sliceable.slicedHandPrefab;
        }

        // Create the slot tracker
        _slots[index] = new GrillSlot(
            item.ingredient,
            item.itemName,
            visualPrefab,   // save whichever visual we used so collect uses it too
            cookTimePerSide,
            burnGraceTime
        );

        // Spawn visual on the grill surface (not parented to avoid scale distortion)
        if (visualPrefab != null && slotPositions[index] != null)
        {
            GameObject visual = Instantiate(visualPrefab);
            visual.transform.position = slotPositions[index].position;
            visual.transform.rotation = slotPositions[index].rotation;

            // Ensure the visual has a collider for raycasting
            if (visual.GetComponent<Collider>() == null
                && visual.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider box = visual.AddComponent<BoxCollider>();
                Renderer rend = visual.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    box.center = visual.transform.InverseTransformPoint(rend.bounds.center);
                    box.size   = visual.transform.InverseTransformVector(rend.bounds.size);
                }
            }

            // Attach the interactive display component
            GrillSlotDisplay display = visual.AddComponent<GrillSlotDisplay>();
            display.Station   = this;
            display.SlotIndex = index;

            _slotVisuals[index] = visual;
        }

        // Clear the player's hand (source scene object is untouched)
        hand.DropItem();

        Debug.Log($"[Grill] Placed {item.itemName} on slot {index}.");
    }

    private void ClearSlot(int index)
    {
        if (_slotVisuals[index] != null)
        {
            Destroy(_slotVisuals[index]);
            _slotVisuals[index] = null;
        }
        _slots[index] = null;
    }

    // ── Slot visuals (color tinting) ─────────────────────────────────────────

    private void UpdateSlotVisual(int index)
    {
        if (_slotVisuals[index] == null) return;

        GrillSlot  slot  = _slots[index];
        GrillState state = slot.GetState();

        Color targetColor;
        switch (state)
        {
            case GrillState.CookingSide1:
                // Raw → half-done tint
                targetColor = Color.Lerp(rawColor, cookedColor, slot.SideProgress * 0.5f);
                break;

            case GrillState.NeedsFlip:
                // Half-done → burnt as grace time runs out
                targetColor = Color.Lerp(cookedColor, burntColor, slot.BurnProgress);
                break;

            case GrillState.CookingSide2:
                // Half-done → fully cooked tint
                targetColor = Color.Lerp(rawColor, cookedColor, 0.5f + slot.SideProgress * 0.5f);
                break;

            case GrillState.Cooked:
                // Cooked → burnt as grace time runs out
                targetColor = Color.Lerp(cookedColor, burntColor, slot.BurnProgress);
                break;

            case GrillState.Burnt:
                targetColor = burntColor;
                break;

            default:
                targetColor = rawColor;
                break;
        }

        Renderer[] renderers = _slotVisuals[index].GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Use .material.color which auto-maps to the correct shader property
            // (_BaseColor for URP Lit, _Color for Standard, etc.)
            r.material.color = targetColor;
        }
    }

    // ── Hint UI (flip / collect prompts) ─────────────────────────────────────

    private void UpdateHintUI()
    {
        if (hintText == null) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) { hintText.text = string.Empty; return; }

        Camera cam = Camera.main;
        if (cam == null)  { hintText.text = string.Empty; return; }

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            // Check the hit object and its parents for a GrillSlotDisplay
            GrillSlotDisplay display = hit.collider.GetComponent<GrillSlotDisplay>()
                                    ?? hit.collider.GetComponentInParent<GrillSlotDisplay>();
            if (display != null && display.Station == this)
            {
                GrillSlot slot = _slots[display.SlotIndex];
                if (slot != null)
                {
                    GrillState state = slot.GetState();
                    if (state == GrillState.NeedsFlip)
                    {
                        hintText.text = "Press [F] to Flip";
                        return;
                    }
                    if (state == GrillState.Cooked)
                    {
                        hintText.text = "Click to Collect";
                        return;
                    }
                    if (state == GrillState.Burnt)
                    {
                        hintText.text = "Burnt! Click to Discard";
                        return;
                    }
                    if (state == GrillState.CookingSide1 || state == GrillState.CookingSide2)
                    {
                        hintText.text = "Cooking...";
                        return;
                    }
                }
            }
        }

        hintText.text = string.Empty;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int FindFreeSlot()
    {
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] == null) return i;
        return -1;
    }
}
