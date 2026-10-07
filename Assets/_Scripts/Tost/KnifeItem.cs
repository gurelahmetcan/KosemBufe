/// <summary>
/// Marker component for the kitchen knife.
/// Extends InteractableItem so PlayerHand can pick it up.
/// TostBread and SucukItem check for this type in the player's hand
/// to decide whether knife-specific interactions are available.
/// </summary>
namespace Bufe
{
    using UnityEngine;

    public class KnifeItem : InteractableItem
    {
        [Tooltip("Return time after a cut. 0 = stays in hand.")]
        [SerializeField] private float returnFlightDuration = 0.45f;

        /// <summary>
        /// Called by whatever the knife just cut: the knife leaves the hand and glides
        /// back to where it lives.
        ///
        /// Why this is automatic. Ingredient storages only respond to an empty hand
        /// (see <see cref="IngredientStorage.CanInteract"/>), so a player still holding
        /// the knife clicks the bread rack and simply nothing happens - no outline, no
        /// hint, no reason given. The knife has exactly one job and it has just been
        /// done, so putting it away is what the player was going to have to do anyway.
        /// Doing it for them removes a dead end rather than taking away a decision.
        ///
        /// It also puts the knife back somewhere findable instead of wherever it was
        /// dropped, which matters because there is only one of them in the büfe.
        /// </summary>
        public void ReturnAfterCut(PlayerHand hand)
        {
            if (returnFlightDuration <= 0f) return;
            if (hand == null || hand.HeldItem != this) return;

            ReturnHomeOnReset home = GetComponent<ReturnHomeOnReset>();
            if (home == null)
            {
                // Nothing on this knife knows where it belongs. Leaving it in the hand
                // is better than releasing it into limbo.
                Debug.LogWarning("[KnifeItem] No ReturnHomeOnReset on the knife - it has " +
                                 "nowhere to fly back to, so it stays in the hand.", this);
                return;
            }

            hand.Release();
            home.FlyHome(returnFlightDuration);
        }
    }
}
