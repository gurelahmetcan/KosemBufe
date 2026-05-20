using System.Collections.Generic;
using System.Linq;

namespace Bufe
{
    public class Order
    {
        public List<OrderSlot> slots;
        public bool IsComplete => slots.All(s => s.isFulfilled);
        public float TotalPrice => slots.Sum(s => s.target.price);
    }
}