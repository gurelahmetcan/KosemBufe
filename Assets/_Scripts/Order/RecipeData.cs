using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "KosemBufe/Recipe")]
    public class RecipeData : ScriptableObject
    {
        [Tooltip("Display name for this recipe (e.g. 'Tost + Ayran').")]
        public string recipeName;

        [Tooltip("Items that make up this order. Each item becomes one slot the player must fulfill.")]
        public List<OrderItem> items;
    }
}
