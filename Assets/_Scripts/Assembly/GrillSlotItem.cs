using UnityEngine;
using DG.Tweening;

/// <summary>
/// Runtime-added component placed on each Ingredient sitting on a Grill slot.
/// Tracks the per-item cooking phase (CookingSide1 → NeedsFlip → CookingSide2
/// → Cooked → Burnt), drives visual color tinting, and handles the player's
/// click / flip interactions for the specific slot.
///
/// Created and configured by Grill.PlaceOnSlot().
///
/// Interaction flow:
///   - Hand holds a Grillable ingredient → click any slot item → delegates
///     back to the owning Grill so the new ingredient lands on the next free
///     slot (natural UX — clicking anywhere on the grill places).
///   - Empty hand + state is Cooked or Burnt → click this item → collect it.
///   - Hover + press F + state is NeedsFlip → flip this item.
/// </summary>
public class GrillSlotItem : MonoBehaviour, IAssemblyInteractable, IFlippable
{
    // ── References set by the Grill on creation ─────────────────────────────

    private Grill      _grill;
    private int        _slotIndex;
    private Ingredient _ingredient;

    private float _cookTimePerSide;
    private float _burnGraceTime;

    // ── Runtime state ────────────────────────────────────────────────────────

    private float _sideTime;
    private bool  _flipped;
    private bool  _isFlipping;

    // ── Visual ───────────────────────────────────────────────────────────────

    private Renderer[] _renderers;

    // ── Public read-only ─────────────────────────────────────────────────────

    public int        SlotIndex  => _slotIndex;
    public Ingredient Ingredient => _ingredient;

    /// <summary>0–1 progress on the current side (clamped).</summary>
    public float SideProgress => Mathf.Clamp01(_sideTime / _cookTimePerSide);

    /// <summary>0–1 progress toward burning once the current side is done.</summary>
    public float BurnProgress => Mathf.Clamp01((_sideTime - _cookTimePerSide) / _burnGraceTime);

    // ── Init ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by Grill immediately after AddComponent. Assigns ownership,
    /// caches renderers, and applies the initial raw tint.
    /// </summary>
    public void Init(Grill grill, int slotIndex, Ingredient ingredient,
                     float cookTimePerSide, float burnGraceTime)
    {
        _grill           = grill;
        _slotIndex       = slotIndex;
        _ingredient      = ingredient;
        _cookTimePerSide = cookTimePerSide;
        _burnGraceTime   = burnGraceTime;

        _renderers = GetComponentsInChildren<Renderer>();
        ApplyColor(_grill.rawColor);
    }

    // ── State query ──────────────────────────────────────────────────────────

    public GrillSlotState GetState()
    {
        if (!_flipped)
        {
            if (_sideTime < _cookTimePerSide)                  return GrillSlotState.CookingSide1;
            if (_sideTime < _cookTimePerSide + _burnGraceTime) return GrillSlotState.NeedsFlip;
            return GrillSlotState.Burnt;
        }
        else
        {
            if (_sideTime < _cookTimePerSide)                  return GrillSlotState.CookingSide2;
            if (_sideTime < _cookTimePerSide + _burnGraceTime) return GrillSlotState.Cooked;
            return GrillSlotState.Burnt;
        }
    }

    // ── Per-frame tick ───────────────────────────────────────────────────────

    private void Update()
    {
        if (GetState() != GrillSlotState.Burnt)
            _sideTime += Time.deltaTime;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_renderers == null || _grill == null) return;

        Color target;
        switch (GetState())
        {
            case GrillSlotState.CookingSide1:
                target = Color.Lerp(_grill.rawColor, _grill.cookedColor, SideProgress * 0.5f);
                break;
            case GrillSlotState.NeedsFlip:
                target = Color.Lerp(_grill.cookedColor, _grill.burntColor, BurnProgress);
                break;
            case GrillSlotState.CookingSide2:
                target = Color.Lerp(_grill.rawColor, _grill.cookedColor, 0.5f + SideProgress * 0.5f);
                break;
            case GrillSlotState.Cooked:
                target = Color.Lerp(_grill.cookedColor, _grill.burntColor, BurnProgress);
                break;
            case GrillSlotState.Burnt:
                target = _grill.burntColor;
                break;
            default:
                target = _grill.rawColor;
                break;
        }

        ApplyColor(target);
    }

    private void ApplyColor(Color color)
    {
        if (_renderers == null) return;
        foreach (Renderer r in _renderers)
            if (r != null) r.material.color = color;
    }

    // ── IAssemblyInteractable ────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // Holding an ingredient → delegate to grill (place on next free slot)
        if (!hand.IsHandEmpty)
            return _grill != null && _grill.CanInteract(hand);

        // Empty hand → can collect once fully cooked or burnt (not mid-flip)
        if (_isFlipping) return false;
        GrillSlotState state = GetState();
        return state == GrillSlotState.Cooked || state == GrillSlotState.Burnt;
    }

    public void Interact(PlayerHand hand)
    {
        // Holding an ingredient → hand off to the grill for next-slot placement
        if (!hand.IsHandEmpty)
        {
            _grill.Interact(hand);
            return;
        }

        // Empty hand + Cooked/Burnt → collect this slot's ingredient
        Collect(hand);
    }

    // ── Collect ──────────────────────────────────────────────────────────────

    private void Collect(PlayerHand hand)
    {
        // Apply the final CookState based on phase
        _ingredient.cookState = GetState() == GrillSlotState.Burnt
            ? CookState.Burnt
            : CookState.Cooked;

        // Unparent from the grill slot
        _ingredient.transform.SetParent(null);

        // NOTE: colliders are already enabled from PlaceOnSlot — no need to
        // re-enable them here. PlayerHand.PickUp() disables them as its first
        // operation, so they are never active while the item moves to holdPoint.

        // Free the slot on the grill
        _grill.FreeSlot(_slotIndex);

        // Hand the ingredient to the player
        hand.PickUp(_ingredient);

        Debug.Log($"[Grill] Collected slot {_slotIndex}: '{_ingredient.itemName}' ({_ingredient.cookState})");

        // Remove self — the ingredient is now in the player's hand
        Destroy(this);
    }

    // ── IFlippable ───────────────────────────────────────────────────────────

    public bool CanFlip()
    {
        if (_isFlipping) return false;
        return GetState() == GrillSlotState.NeedsFlip;
    }

    public void Flip()
    {
        if (!CanFlip()) return;

        _flipped  = true;
        _sideTime = 0f;

        PlayFlipAnimation();

        Debug.Log($"[Grill] Flipped slot {_slotIndex}.");
    }

    // ── Flip animation ───────────────────────────────────────────────────────

    private void PlayFlipAnimation()
    {
        _isFlipping = true;

        // Quick jump + 180° rotation on the local X axis
        Vector3 startPos = transform.localPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(startPos.y + 3f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(transform.DOLocalRotate(new Vector3(180f, 0f, 0f), 0.3f, RotateMode.LocalAxisAdd)
                          .SetEase(Ease.InOutQuad));
        seq.Append(transform.DOLocalMoveY(startPos.y, 0.15f).SetEase(Ease.InQuad));
        seq.OnComplete(() => _isFlipping = false);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
