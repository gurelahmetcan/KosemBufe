using UnityEngine;

/// <summary>
/// ScriptableObject that defines an ingredient storage source — a container
/// (e.g. a bag of potatoes, a block of cheese) from which the player can
/// repeatedly pull individual ingredient items.
///
/// Assign one of these to an IngredientStorage MonoBehaviour placed in the scene.
/// The storage is infinite: each interaction spawns a fresh instance of ingredientPrefab.
///
/// Create via: Assets → Create → KosemBufe → Ingredient Storage
/// </summary>
[CreateAssetMenu(fileName = "New IngredientStorage", menuName = "KosemBufe/Ingredient Storage")]
public class IngredientStorageData : ScriptableObject
{
    [Tooltip("Human-readable name shown on the storage container label (e.g. 'Potato Sack', 'Cheese Block').")]
    public string storageName = "Ingredient Storage";

    [Tooltip("Prefab of the individual Ingredient spawned each time the player interacts. " +
             "Must have an InteractableItem (or subclass) component attached.")]
    public GameObject ingredientPrefab;

    [Tooltip("Optional icon used in any UI that references this storage (e.g. recipe guides, shop UI).")]
    public Sprite icon;
}
