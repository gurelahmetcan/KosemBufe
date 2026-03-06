using UnityEngine;

/// <summary>
/// Represents one ingredient slot inside the vending machine.
/// Each slot has an auger spiral (coil) that rotates to push the item forward,
/// and a prefab that gets instantiated as a physics-based Rigidbody when dispensed.
///
/// Setup:
///   1. Place this on a child GameObject of the vending machine.
///   2. Assign the spiralTransform to the coil mesh that visually rotates.
///   3. Assign itemPrefab — must have a Rigidbody and Collider.
///   4. Assign spawnPoint — empty Transform at the end of the coil where the item drops.
/// </summary>
public class VendingMachineSlot : MonoBehaviour
{
    [Tooltip("Display name of the ingredient (e.g. 'Cheese', 'Tomato', 'Sausage').")]
    public string ingredientName = "Ingredient";

    [Tooltip("Sprite icon shown on the corresponding button.")]
    public Sprite icon;

    [Tooltip("Prefab to instantiate when dispensing. Must have Rigidbody + Collider.")]
    public GameObject itemPrefab;

    [Tooltip("HoldableItem used for the player's hand display when auto-picked up. Does not need Rigidbody.")]
    public HoldableItem holdableItem;

    [Tooltip("The auger spiral Transform that rotates during dispensing.")]
    public Transform spiralTransform;

    [Tooltip("Empty Transform at the coil exit where the item spawns.")]
    public Transform spawnPoint;

    [Tooltip("Rotation axis for the spiral (local space). Default: forward (Z).")]
    public Vector3 rotationAxis = Vector3.forward;

    [Tooltip("Degrees per full dispense cycle. One full turn = 360.")]
    public float degreesPerDispense = 360f;

    [HideInInspector]
    public int currentStock;

    /// <summary>Initialize stock from config at runtime.</summary>
    public void InitStock(int max)
    {
        currentStock = max;
    }

    /// <summary>Returns true if at least one item remains.</summary>
    public bool HasStock => currentStock > 0;

    /// <summary>
    /// Rotate the spiral by a normalized fraction (0–1) of the full dispense cycle.
    /// Called every frame during the DISPENSING state.
    /// </summary>
    public void RotateSpiral(float deltaNormalized)
    {
        if (spiralTransform == null) return;
        float angle = degreesPerDispense * deltaNormalized;
        spiralTransform.Rotate(rotationAxis, angle, Space.Self);
    }

    /// <summary>
    /// Spawn the ingredient item at the spawn point with physics enabled.
    /// Returns the spawned GameObject so the machine can track it.
    /// </summary>
    public GameObject SpawnItem()
    {
        if (itemPrefab == null || spawnPoint == null) return null;

        currentStock--;

        GameObject item = Instantiate(itemPrefab, spawnPoint.position, spawnPoint.rotation);

        // Guarantee physics components
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null)
            rb = item.AddComponent<Rigidbody>();

        rb.isKinematic = false;

        // Ensure at least one collider exists
        if (item.GetComponent<Collider>() == null && item.GetComponentInChildren<Collider>() == null)
        {
            MeshFilter mf = item.GetComponent<MeshFilter>() ?? item.GetComponentInChildren<MeshFilter>();
            if (mf != null)
            {
                MeshCollider mc = item.AddComponent<MeshCollider>();
                mc.convex = true;
            }
            else
            {
                item.AddComponent<BoxCollider>();
            }
        }

        return item;
    }
}
