using System.Collections.Generic;
using System.Linq;

namespace Bufe
{
    public class Order
    {
        public List<OrderSlot> slots;
        public bool IsComplete => slots.All(s => s.isFulfilled);
        public float TotalPrice => slots.Sum(s => s.target.price);

        public static Order FromRecipe(OrderTemplate template)
        {
            var order = new Order();
            order.slots = template.items.Select(item => new OrderSlot { target = item }).ToList();
            return order;
        }
    }
}