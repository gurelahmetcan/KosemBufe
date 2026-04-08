using UnityEngine;

/// <summary>
/// Grill appliance — accepts a single raw Ingredient with the Grillable flag,
/// cooks it over time (Raw → Cooked), and burns it if left too long (Cooked → Burnt).
///
/// The ingredient remains visible on the grill surface at itemSlot and is
/// color-tinted to reflect cooking progress.
///
/// For multiple grill spots, place multiple Grill GameObjects on the surface.
///
/// Setup:
///   1. Place a GameObject with a Collider (the grill surface).
///   2. Add this component.
///   3. Create an empty child Transform and assign it as itemSlot.
///   4. On ingredient prefabs: set supportedProcesses = Grillable.
/// </summary>
public class Grill : Appliance
{
    [Header("Grill Setup")]
    [Tooltip("Position where the ingredient sits on the grill surface.")]
    public Transform itemSlot;

    [Header("Visual — Cook Colors")]
    [Tooltip("Tint applied to raw / just-placed items.")]
    public Color rawColor = new Color(0.85f, 0.35f, 0.35f);

    [Tooltip("Tint applied to fully-cooked items.")]
    public Color cookedColor = new Color(0.55f, 0.35f, 0.18f);

    [Tooltip("Tint applied to burnt items.")]
    public Color burntColor = new Color(0.15f, 0.10f, 0.05f);

    // ── Runtime ──────────────────────────────────────────────────────────────

    private Ingredient _ingredient;
    private Renderer[] _itemRenderers;

    // ── Appliance overrides ──────────────────────────────────────────────────

    protected override bool CanAccept(InteractableItem item)
    {
        return item is Ingredient && item.supportedProcesses.HasFlag(ProcessType.Grillable);
    }

    protected override void OnItemInserted()
    {
        _ingredient = _storedItem as Ingredient;

        // Position ingredient on the grill surface
        if (itemSlot != null)
        {
            _storedItem.transform.position = itemSlot.position;
            _storedItem.transform.rotation = itemSlot.rotation;
        }

        // Cache renderers for color tinting
        _itemRenderers = _storedItem.GetComponentsInChildren<Renderer>();
        ApplyColor(rawColor);

        Debug.Log($"[Grill] Inserted '{_ingredient.itemName}' — cooking for {processTime}s");
    }

    protected override void OnProcessingComplete()
    {
        _ingredient.cookState = CookState.Cooked;
        Debug.Log($"[Grill] '{_ingredient.itemName}' is COOKED! Collect before it burns ({burnGraceTime}s).");
    }

    protected override void OnBurned()
    {
        _ingredient.cookState = CookState.Burnt;
        Debug.Log($"[Grill] '{_ingredient.itemName}' is BURNT!");
    }

    protected override void OnBeforeCollect()
    {
        _itemRenderers = null;
        _ingredient = null;
    }

    // ── Per-frame visual feedback ────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_itemRenderers == null) return;

        switch (_state)
        {
            case ApplianceState.Processing:
                ApplyColor(Color.Lerp(rawColor, cookedColor, ProcessProgress));
                break;

            case ApplianceState.Done:
                ApplyColor(Color.Lerp(cookedColor, burntColor, BurnProgress));
                break;

            case ApplianceState.Burned:
                ApplyColor(burntColor);
                break;
        }
    }

    private void ApplyColor(Color color)
    {
        if (_itemRenderers == null) return;
        foreach (Renderer r in _itemRenderers)
            r.material.color = color;
    }
}
