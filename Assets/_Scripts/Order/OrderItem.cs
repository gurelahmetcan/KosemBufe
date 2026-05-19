using UnityEngine;

namespace Bufe
{
    [CreateAssetMenu(fileName = "NewOrderItem", menuName = "KosemBufe/New Order Item")]
    public class OrderItem : ScriptableObject
    {
        [Tooltip("Display name shown in Order ")]
        public string itemName;
        
        [Tooltip("How much the player earns for delivering this recipe.")]
        [Min(0f)]
        public float price;
        
        [Tooltip("Order Icon to be shown in UI.")]
        public Sprite icon;
        
        [Tooltip("Item category to decide if prepared or premade.")]
        public Constants.ItemCategory category;
        
        // Only used if category == Prepared
        [Tooltip("Exact ingredients required. Order doesn't matter.")]
        public Constants.IngredientType[] ingredients;
        
        [Tooltip("If true, the order must be cooked.")]
        public bool requiresCooking = false;
    }
}

