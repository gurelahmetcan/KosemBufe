using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toaster station — accepts a loaded HolderItem whose HoldableItem
/// has the Toastable process flag, then launches the toaster minigame.
///
/// Attach this (and a Collider) to the Toaster object in the scene.
/// Assign the ToasterMinigame reference in the inspector.
/// </summary>
public class ToasterStation : Station
{
    [Tooltip("The ToasterMinigame controller in the scene.")]
    public ToasterMinigame minigame;

    public override bool CanInteract(InHandController hand)
    {
        if (hand.HeldItem == null) return false;
        if (!hand.HeldItem.supportedProcesses.HasFlag(ProcessType.Toastable)) return false;

        HolderItem holder = hand.HeldItem.GetComponent<HolderItem>();
        return holder != null && holder.StoredCount > 0;
    }

    public override void Interact(InHandController hand)
    {
        HoldableItem savedItem   = hand.HeldItem;
        HolderItem   savedHolder = savedItem.GetComponent<HolderItem>();

        hand.ClearHoverLabel();
        hand.DropItem();
        hand.InputBlocked = true;

        Debug.Log($"[Toaster] Starting minigame with holder='{savedItem.itemName}', storedCount={savedHolder?.StoredCount}");

        minigame.StartMinigame(success =>
        {
            if (savedItem != null)
            {
                if (savedHolder != null)
                    savedItem.preparedIngredients = new List<IngredientType>(savedHolder.GetIngredients());

                savedItem.cookState = success ? CookState.Cooked : CookState.Burnt;

                Debug.Log($"[Toaster] Minigame done: success={success}, cookState={savedItem.cookState}, ingredients=[{string.Join(", ", savedItem.preparedIngredients)}]");

                hand.PickUp(savedItem);
            }

            hand.InputBlocked = false;
        });
    }
}
