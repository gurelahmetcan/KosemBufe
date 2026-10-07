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
        public OrderTemplate recipe;

        [Tooltip("Relative pick weight.")]
        [Min(1)]
        public int weight;
    }

}