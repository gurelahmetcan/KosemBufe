/// <summary>
/// Pure static utility that assembles dialogue strings from a CustomerInstance.
/// No MonoBehaviour, no side effects — just data in, strings out.
///
/// Normal customer flow:
///   1. Order line     → shown when player clicks customer (+ optional modifier)
///   2. Exit line      → shown after delivery or timeout (positive / negative / timeout)
/// </summary>

using System.Collections.Generic;
using System.Linq;

namespace Bufe
{
    public static class DialogueBuilder
    {
        /// <summary>
        /// The order dialogue shown when the player clicks the customer.
        /// Contains the order phrase and an optional modifier phrase.
        /// </summary>
        public struct OrderDialogue
        {
            public string order;
        }

        /// <summary>
        /// Builds the order dialogue shown when the player clicks the customer.
        /// </summary>
        public static OrderDialogue BuildOrderDialogue(CustomerInstance instance)
        {
            var mood = instance.Mood;

            string orderPhrase = mood.GetOrderPhrase();
            string foodName    = GetOrderFoodName(instance.Order);
            orderPhrase = orderPhrase.Replace("{0}", foodName);

            return new OrderDialogue
            {
                order    = orderPhrase,
            };
        }

        /// <summary>Joins the order's item names into a natural-language list, e.g. "A and B" or "A, B and C".</summary>
        private static string GetOrderFoodName(Order order)
        {
            List<string> names = order?.slots?.Select(s => s.target.itemName).ToList();
            if (names == null || names.Count == 0) return "something";
            if (names.Count == 1) return names[0];

            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];
        }

        /// <summary>
        /// Returns the appropriate exit line based on the order result.
        /// Perfect → positive exit, WrongIngredients/Raw/Burnt → negative exit.
        /// </summary>
        public static string GetExitLine(CustomerInstance instance)
        {
            var mood = instance.Mood;

            return mood.GetExitLine();
        }
    }
}
