namespace Bufe
{
    /// <summary>
    /// A storage the player can reorder stock for via the phone order screen.
    /// Implemented by IngredientStorage and PremadeStorage so PhoneOrderScreen treats both uniformly.
    /// Infinite storages (IsInfinite) are skipped by the order screen.
    /// </summary>
    public interface IRestockable
    {
        string DisplayName { get; }
        UnityEngine.Sprite Icon { get; }
        float UnitCost { get; }
        int StockCount { get; }
        bool IsInfinite { get; }
        OrderCategory Category { get; }
        void AddStock(int amount);
    }

    /// <summary>A single line of a restock order: how many units of one storage to buy.</summary>
    public struct RestockLine
    {
        public IRestockable target;
        public int amount;

        public RestockLine(IRestockable target, int amount)
        {
            this.target = target;
            this.amount = amount;
        }
    }
}
