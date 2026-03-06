using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A physical button on the vending machine panel.
/// Handles click detection, visual lock state, and jam flash effect.
///
/// Setup:
///   1. Attach to a child GameObject of the vending machine with a Collider.
///   2. Set slotIndex to match the corresponding VendingMachineSlot in the slots array.
///   3. Assign the parent VendingMachine reference.
///   4. Optionally assign a Renderer for color feedback and an Image/SpriteRenderer for the icon.
/// </summary>
public class VendingMachineButton : MonoBehaviour
{
    [Tooltip("Index into VendingMachine.slots that this button dispenses.")]
    public int slotIndex;

    [Tooltip("Reference to the parent vending machine.")]
    public VendingMachine machine;

    [Header("Visuals")]
    [Tooltip("Renderer whose material color changes for feedback (lock/jam).")]
    public Renderer buttonRenderer;

    [Tooltip("SpriteRenderer showing the ingredient icon (assigned from the slot's sprite).")]
    public SpriteRenderer iconRenderer;

    [Header("Colors")]
    public Color normalColor  = Color.white;
    public Color lockedColor  = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color jamColor     = Color.red;

    [Header("Jam Flash")]
    [Tooltip("Flashes per second when jammed.")]
    public float flashRate = 4f;

    // ── Runtime ─────────────────────────────────────────────────────────────────

    private bool      _isLocked;
    private Coroutine _flashRoutine;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Auto-assign icon from the slot if available
        if (iconRenderer != null && machine != null &&
            slotIndex >= 0 && slotIndex < machine.slots.Length)
        {
            Sprite slotIcon = machine.slots[slotIndex].icon;
            if (slotIcon != null)
                iconRenderer.sprite = slotIcon;
        }

        SetColor(normalColor);
    }

    // ── Click detection ─────────────────────────────────────────────────────────

    private void OnMouseDown()
    {
        if (_isLocked) return;
        if (machine == null) return;

        machine.RequestDispense(slotIndex);
    }

    // ── Public API ──────────────────────────────────────────────────────────────

    /// <summary>Lock or unlock this button. Locked buttons ignore input.</summary>
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
        if (!locked)
            SetColor(normalColor);
        else if (_flashRoutine == null)
            SetColor(lockedColor);
    }

    /// <summary>Start flashing red to indicate a jam on this slot.</summary>
    public void ShowJamVisual()
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>Stop the jam flash and restore normal color.</summary>
    public void ClearJamVisual()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }
        SetColor(_isLocked ? lockedColor : normalColor);
    }

    // ── Flash coroutine ─────────────────────────────────────────────────────────

    private IEnumerator FlashRoutine()
    {
        float interval = 1f / (flashRate * 2f); // half-period
        bool on = true;

        while (true)
        {
            SetColor(on ? jamColor : lockedColor);
            on = !on;
            yield return new WaitForSeconds(interval);
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private void SetColor(Color color)
    {
        if (buttonRenderer == null) return;

        buttonRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", color);
        buttonRenderer.SetPropertyBlock(_propBlock);
    }
}
