using UnityEngine;

/// <summary>
/// Multi-slot grill surface. Accepts raw Ingredients with the Grillable
/// process flag and cooks them with a flip-based 2-side mechanic.
///
/// Behaviour:
///   - Click the grill with a Grillable ingredient → auto-places on the next
///     free slot (LIFO-ish, first empty index wins).
///   - Each slot cooks independently: CookingSide1 → NeedsFlip → CookingSide2
///     → Cooked → Burnt, driven by a GrillSlotItem component on the ingredient.
///   - Hover an ingredient and press F → flip it (only during NeedsFlip).
///   - Click an ingredient with an empty hand → collect it once Cooked/Burnt.
///
/// Per-slot state, timing, visual tinting, and flip/collect interactions all
/// live on GrillSlotItem so each ingredient manages itself.
///
/// Setup:
///   1. Place a GameObject with a Collider (the grill surface).
///   2. Add this component.
///   3. Create empty child Transforms for each slot position.
///   4. Assign them to slotPositions (length determines capacity).
///   5. On grillable ingredient prefabs: set supportedProcesses = Grillable.
/// </summary>
public class Grill : MonoBehaviour, IAssemblyInteractable
{
    [Header("Slot Positions")]
    [Tooltip("Empty child Transforms marking where ingredients sit on the grill. " +
             "Array length determines capacity.")]
    public Transform[] slotPositions;

    [Header("Timing")]
    [Tooltip("Seconds each side needs to cook before the next phase.")]
    public float cookTimePerSide = 5f;

    [Tooltip("Seconds of grace after a side is done before the ingredient burns.")]
    public float burnGraceTime = 5f;

    [Header("Visual — Cook Colors")]
    [Tooltip("Tint applied to raw / just-placed ingredients.")]
    public Color rawColor = new Color(0.85f, 0.35f, 0.35f);

    [Tooltip("Tint applied to fully-cooked ingredients.")]
    public Color cookedColor = new Color(0.55f, 0.35f, 0.18f);

    [Tooltip("Tint applied to burnt ingredients.")]
    public Color burntColor = new Color(0.15f, 0.10f, 0.05f);

    // ── Runtime state ────────────────────────────────────────────────────────

    private GrillSlotItem[] _slots;

    public int Capacity => slotPositions?.Length ?? 0;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _slots = new GrillSlotItem[Capacity];
    }

    // ── IAssemblyInteractable ────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // Only handles placement — collect/flip is done via GrillSlotItem on each slot.
        if (hand.IsHandEmpty) return false;
        if (!(hand.HeldItem is Ingredient)) return false;
        if (!hand.HeldItem.supportedProcesses.HasFlag(ProcessType.Grillable)) return false;
        return FindFreeSlot() >= 0;
    }

    public void Interact(PlayerHand hand)
    {
        int slot = FindFreeSlot();
        if (slot < 0) return;

        Ingredient ingredient = hand.HeldItem as Ingredient;
        if (ingredient == null) return;

        PlaceOnSlot(slot, ingredient, hand);
    }

    // ── Public API (called by GrillSlotItem on collect) ─────────────────────

    /// <summary>
    /// Clears the given slot. Called by GrillSlotItem when the player collects it.
    /// </summary>
    public void FreeSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return;
        _slots[index] = null;
    }

    // ── Placement ────────────────────────────────────────────────────────────

    private void PlaceOnSlot(int index, Ingredient ingredient, PlayerHand hand)
    {
        // Release from the player's hand — stays dormant (kinematic, colliders off).
        hand.Release();

        // Parent and position on the slot transform.
        Transform slotT = slotPositions[index];
        ingredient.transform.SetParent(slotT);
        ingredient.transform.position = slotT.position;
        ingredient.transform.rotation = slotT.rotation;

        // Re-enable colliders so the player can click / raycast this ingredient.
        foreach (Collider col in ingredient.GetComponentsInChildren<Collider>())
            col.enabled = true;

        // Attach the per-slot cooking component and track it.
        GrillSlotItem slotItem = ingredient.gameObject.AddComponent<GrillSlotItem>();
        slotItem.Init(this, index, ingredient, cookTimePerSide, burnGraceTime);

        _slots[index] = slotItem;

        Debug.Log($"[Grill] Placed '{ingredient.itemName}' on slot {index}.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private int FindFreeSlot()
    {
        if (_slots == null) return -1;
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] == null) return i;
        return -1;
    }
}
