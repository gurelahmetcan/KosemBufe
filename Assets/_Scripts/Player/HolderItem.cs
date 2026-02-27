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
public class HolderItem : MonoBehaviour
{
    [Tooltip("Maximum number of items this holder can store.")]
    public int maxSlots = 3;

    [Tooltip("Child TextMeshPro label that shows slot info on hover. " +
             "Position it freely in the prefab; start it disabled.")]
    public TextMeshPro hoverLabel;

    private readonly List<HoldableItem> _storedItems = new List<HoldableItem>();

    public int  StoredCount => _storedItems.Count;
    public bool IsFull      => _storedItems.Count >= maxSlots;

    public void AddItem(HoldableItem item)
    {
        if (!IsFull)
        {
            _storedItems.Add(item);
            RefreshLabelText();
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
