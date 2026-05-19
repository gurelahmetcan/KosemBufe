using System.Collections.Generic;
using Bufe;
using TMPro;
using UnityEngine;

/// <summary>
/// Add-on component that turns any InteractableItem into a container (e.g. Bread, Bowl, Shish).
///
/// Interactions:
///  - Sliced item in hand + click holder → item stored, hand cleared.
///  - Empty hand            + click holder → pick up holder (IAssemblyInteractable returns false,
///                                           PlayerHand falls through to InteractableItem pickup).
///  - Right-click while holding holder    → stored items deleted, hand cleared (handled by PlayerHand.Drop).
/// </summary>
[RequireComponent(typeof(InteractableItem))]
public class HolderItem : MonoBehaviour, IAssemblyInteractable
{
    [Tooltip("Maximum number of items this holder can store.")]
    public int maxSlots = 3;

    [Tooltip("Child TextMeshPro label that shows slot info on hover.")]
    public TextMeshPro hoverLabel;

    private readonly List<InteractableItem> _storedItems = new();

    public int  StoredCount => _storedItems.Count;
    public bool IsFull      => _storedItems.Count >= maxSlots;

    // ── IAssemblyInteractable ────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        return hand.HeldItem is Ingredient && !IsFull;
    }

    public void Interact(PlayerHand hand)
    {
        InteractableItem item = hand.Release();
        AddItem(item);
        if (item != null) item.gameObject.SetActive(false);
    }

    // ── Ingredient data ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a list of ingredient types from all stored items plus the holder's own.
    /// Used by the toaster flow to build the result item's ingredient list.
    /// </summary>
    public List<Constants.IngredientType> GetIngredients()
    {
        var list = new List<Constants.IngredientType>();

        Constants.IngredientType own = GetComponent<InteractableItem>().ingredient;
        if (own != Constants.IngredientType.None)
            list.Add(own);

        foreach (InteractableItem item in _storedItems)
        {
            if (item.ingredient != Constants.IngredientType.None)
                list.Add(item.ingredient);
        }

        return list;
    }

    public void AddItem(InteractableItem item)
    {
        if (item == null || IsFull) return;
        _storedItems.Add(item);
        RefreshLabelText();
    }

    public void ClearItems()
    {
        _storedItems.Clear();
        RefreshLabelText();
    }

    // ── Hover label ──────────────────────────────────────────────────────────

    public void ShowLabel()
    {
        if (hoverLabel == null) return;
        RefreshLabelText();
        hoverLabel.gameObject.SetActive(true);
    }

    public void HideLabel()
    {
        if (hoverLabel != null)
            hoverLabel.gameObject.SetActive(false);
    }

    private void RefreshLabelText()
    {
        if (hoverLabel == null) return;
        string name = GetComponent<InteractableItem>().itemName;
        hoverLabel.text = $"{name}: {StoredCount}/{maxSlots}";
    }
}
