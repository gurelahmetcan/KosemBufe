using UnityEngine;

/// <summary>
/// Tracks the cooking progress of a single item sitting on the grill.
/// Each slot records time spent on the current side and whether a flip
/// has occurred.  State is derived purely from these values.
///
/// Ticked every frame by GrillStation.Update().
/// </summary>
public class GrillSlot
{
    // ── Saved item data (needed to create the output when collected) ─────────

    public Ingredient  SavedIngredient { get; }
    public string      SavedItemName   { get; }
    public GameObject  SavedHandPrefab { get; }

    // ── Cooking state ────────────────────────────────────────────────────────

    /// <summary>Seconds elapsed on the current side.</summary>
    public float SideTime { get; private set; }

    /// <summary>True after the player has flipped the item.</summary>
    public bool  Flipped  { get; private set; }

    private readonly float _cookTime;
    private readonly float _burnGrace;

    public GrillSlot(Ingredient ingredient, string itemName, GameObject handPrefab,
                     float cookTime, float burnGrace)
    {
        SavedIngredient = ingredient;
        SavedItemName   = itemName;
        SavedHandPrefab = handPrefab;
        _cookTime       = cookTime;
        _burnGrace      = burnGrace;
    }

    // ── Per-frame update ─────────────────────────────────────────────────────

    public void Tick(float deltaTime)
    {
        if (GetState() == GrillState.Burnt) return;
        SideTime += deltaTime;
    }

    // ── State query ──────────────────────────────────────────────────────────

    public GrillState GetState()
    {
        if (!Flipped)
        {
            if (SideTime < _cookTime)                return GrillState.CookingSide1;
            if (SideTime < _cookTime + _burnGrace)   return GrillState.NeedsFlip;
            return GrillState.Burnt;
        }
        else
        {
            if (SideTime < _cookTime)                return GrillState.CookingSide2;
            if (SideTime < _cookTime + _burnGrace)   return GrillState.Cooked;
            return GrillState.Burnt;
        }
    }

    /// <summary>
    /// Attempts to flip the item.  Only succeeds when the state is NeedsFlip.
    /// Resets the side timer so the second side cooks from zero.
    /// </summary>
    public bool TryFlip()
    {
        if (GetState() != GrillState.NeedsFlip) return false;
        Flipped  = true;
        SideTime = 0f;
        return true;
    }

    // ── Helper ratios for visual interpolation ───────────────────────────────

    /// <summary>0 → 1 progress on the current side (clamped).</summary>
    public float SideProgress => Mathf.Clamp01(SideTime / _cookTime);

    /// <summary>
    /// 0 → 1 progress toward burning once the side is done.
    /// Only meaningful in NeedsFlip or Cooked states.
    /// </summary>
    public float BurnProgress => Mathf.Clamp01((SideTime - _cookTime) / _burnGrace);
}
