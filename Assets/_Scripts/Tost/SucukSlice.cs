using UnityEngine;

/// <summary>
/// Added at runtime to each sucuk slice the moment it is cut on a cutting board
/// (see <see cref="PrepStation"/>). While the slice rests on the board's slice
/// anchor, an empty-hand left-click drops it straight into the open bread on the
/// same board. If there is no open bread with a free socket, the click falls
/// through to the normal pick-up path (the slice is still a plain Ingredient).
/// Slices cut in the hand (away from a board) never get this component.
/// </summary>
namespace Bufe
{
    public class SucukSlice : MonoBehaviour, IAssemblyInteractable
    {
        private PrepStation _station;
        private Ingredient  _ingredient;

        public void Bind(PrepStation station)
        {
            _station    = station;
            _ingredient = GetComponent<Ingredient>();
        }

        public bool CanInteract(PlayerHand hand)
        {
            return hand.IsHandEmpty
                && _station != null
                && _ingredient != null
                && _station.CanDepositSlice(_ingredient.ingredientType);
        }

        public void Interact(PlayerHand hand)
        {
            if (_station != null)
                _station.DepositSlice(this);
        }

        /// <summary>Detaches this slice from its board (it has become part of the bread).</summary>
        public void ClearStation()
        {
            _station = null;
        }
    }
}
