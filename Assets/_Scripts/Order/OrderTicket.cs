using System.Linq;
using TMPro;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// In-world paper note placed on the counter, listing the active order's
    /// remaining (unfulfilled) items. Spawned/destroyed by OrderManager.
    /// </summary>
    public class OrderTicket : MonoBehaviour
    {
        [Tooltip("Lists the remaining items.")]
        [SerializeField] private TextMeshPro ticketText;

        /// <summary>Refreshes the ticket to show only the order's unfulfilled slots.</summary>
        public void SetOrder(Order order)
        {
            if (ticketText == null || order == null) return;

            ticketText.text = string.Join("\n",
                order.slots.Where(s => !s.isFulfilled).Select(s => s.target.itemName));
        }
    }
}
