using UnityEngine;

namespace Bufe
{
    [CreateAssetMenu(fileName = "NewOrderItem", menuName = "KosemBufe/New Order Item")]
    public class OrderItem : ScriptableObject
    {
        [Tooltip("Name shown in the order.")]
        public string itemName;
        
        [Tooltip("Sale price.")]
        [Min(0f)]
        public float price;
        
        [Tooltip("Order icon.")]
        public Sprite icon;
        
        [Tooltip("Prepared or premade.")]
        public Constants.ItemCategory category;
        
        // Only used if category == Prepared
        [Tooltip("Required ingredients, any order.")]
        public Constants.IngredientType[] ingredients;
        
        [Tooltip("Must be cooked.")]
        public bool requiresCooking = false;
    }
}

