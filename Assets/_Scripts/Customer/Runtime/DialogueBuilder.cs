/// <summary>
/// Pure static utility that assembles dialogue strings from a CustomerInstance.
/// No MonoBehaviour, no side effects — just data in, strings out.
///
/// Normal customer flow:
///   1. Opening line   → shown when customer spawns
///   2. Order line     → shown when player clicks customer (+ optional modifier)
///   3. Exit line      → shown after delivery or timeout (positive / negative / timeout)
/// </summary>

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
        /// Returns the opening line for when the customer spawns.
        /// </summary>
        public static string GetOpeningLine(CustomerInstance instance)
        {
            return instance.Mood.GetOpeningLine();
        }

        /// <summary>
        /// Builds the order dialogue shown when the player clicks the customer.
        /// </summary>
        public static OrderDialogue BuildOrderDialogue(CustomerInstance instance)
        {
            var mood = instance.Mood;

            string orderPhrase = mood.GetOrderPhrase();
            orderPhrase = orderPhrase.Replace("{0}", instance.Order.itemName);

            return new OrderDialogue
            {
                order    = orderPhrase,
            };
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
