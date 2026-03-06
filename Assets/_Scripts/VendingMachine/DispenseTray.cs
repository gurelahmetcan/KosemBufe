using System;
using UnityEngine;

/// <summary>
/// Trigger zone at the bottom of the vending machine that detects
/// when a dispensed item has landed and when it has been picked up.
///
/// Setup:
///   1. Attach to the dispenser tray GameObject.
///   2. Add a Collider with isTrigger = true sized to the tray area.
///   3. Dispensed items must have a Rigidbody (handled by VendingMachineSlot).
/// </summary>
[RequireComponent(typeof(Collider))]
public class DispenseTray : MonoBehaviour
{
    /// <summary>Fired when a Rigidbody item enters the tray trigger.</summary>
    public event Action<GameObject> OnItemLanded;

    /// <summary>True when at least one item is sitting in the tray.</summary>
    public bool HasItem => _itemCount > 0;

    private int _itemCount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            _itemCount++;
            OnItemLanded?.Invoke(other.attachedRigidbody.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null)
            _itemCount = Mathf.Max(0, _itemCount - 1);
    }

    /// <summary>Reset count if tracked objects are destroyed while inside.</summary>
    public void ResetCount()
    {
        _itemCount = 0;
    }
}
