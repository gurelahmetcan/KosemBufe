using UnityEngine;

namespace Bufe
{
    public class TrashBin : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [SerializeField] private string hoverLabel = "Trash Bin";
        
        public string HoverLabel => hoverLabel;

        public bool CanInteract(PlayerHand hand)
        {
            return !hand.IsHandEmpty;
        }

        public void Interact(PlayerHand hand)
        {
            hand.DestroyHeldItem();
        }
    }
}