using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Add-on component that turns any HoldableItem into a container that stores
/// other items (e.g. Bread, Bowl, Shish).
///
/// Requires a HoldableItem on the same GameObject — that component provides
/// the item name and hand prefab used when the holder itself is picked up.
///
/// Each holder prefab should have a child TextMeshPro label assigned to hoverLabel.
/// Position and scale the child freely per prefab in the editor.
/// SlicingController calls ShowLabel() / HideLabel() on hover.
///
/// Interactions (handled by SlicingController):
///  - Sliced item in hand + click holder  → item stored, hand cleared.
///  - Empty hand            + click holder → pick up holder (+ stored items).
///  - Right-click while holding           → stored items deleted, hand cleared.
/// </summary>
[RequireComponent(typeof(HoldableItem))]
public class HolderItem : MonoBehaviour, IInteractable
{
    // ── IInteractable ────────────────────────────────────────────────────────

    public bool CanInteract(InHandController hand)
    {
        return hand.HeldItem != null && hand.IsSliced && !IsFull;
    }

    public void Interact(InHandController hand)
    {
        AddItem(hand.HeldItem);
        hand.DropItem();
    }

    [Tooltip("Maximum number of items this holder can store.")]
    public int maxSlots = 3;

    [Tooltip("Child TextMeshPro label that shows slot info on hover. " +
             "Position it freely in the prefab; start it disabled.")]
    public TextMeshPro hoverLabel;

    private readonly List<HoldableItem> _storedItems = new List<HoldableItem>();

    public int  StoredCount => _storedItems.Count;
    public bool IsFull      => _storedItems.Count >= maxSlots;

    /// <summary>
    /// Returns a list of Ingredient enums from all stored items.
    /// Used by the toaster flow to transfer ingredients to the result item.
    /// </summary>
    public List<Ingredient> GetIngredients()
    {
        var list = new List<Ingredient>();

        // Include the holder's own ingredient (e.g. bread = Ekmek)
        Ingredient own = GetComponent<HoldableItem>().ingredient;
        if (own != Ingredient.None)
            list.Add(own);

        foreach (HoldableItem item in _storedItems)
        {
            if (item.ingredient != Ingredient.None)
                list.Add(item.ingredient);
        }

        Debug.Log($"[HolderItem] GetIngredients → own={own}, stored={_storedItems.Count}, total=[{string.Join(", ", list)}]");
        return list;
    }

    public void AddItem(HoldableItem item)
    {
        if (!IsFull)
        {
            _storedItems.Add(item);
            Debug.Log($"[HolderItem] Added '{item.itemName}' ({item.ingredient}) → {StoredCount}/{maxSlots}");
            RefreshLabelText();
        }
        else
        {
            Debug.Log($"[HolderItem] FULL — cannot add '{item.itemName}' ({StoredCount}/{maxSlots})");
        }
    }

    public void ClearItems()
    {
        _storedItems.Clear();
        RefreshLabelText();
    }

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
        string itemName = GetComponent<HoldableItem>().itemName;
        hoverLabel.text = $"{itemName}: {StoredCount}/{maxSlots}";
    }
}
