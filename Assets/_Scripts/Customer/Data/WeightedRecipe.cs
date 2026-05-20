using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A recipe paired with a selection weight.
/// Higher weight = more likely to be chosen by this archetype.
/// </summary>

namespace Bufe
{
    [Serializable]
    public struct WeightedRecipe
    {
        [Tooltip("The recipe this customer might order.")]
        public RecipeData recipe;

        [Tooltip("Relative weight for random selection (e.g. 3 = three times as likely as weight 1).")]
        [Min(1)]
        public int weight;
    }

}