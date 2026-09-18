using UnityEngine;

namespace Bufe
{
    public class TrashBin : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [SerializeField] private string hoverLabel = "Trash Bin";

        [Tooltip("Shown instead of the label above while holding something that cannot be " +
                 "thrown away (the knife, a quest item).")]
        [SerializeField] private string refusedLabel = "Trash Bin — you still need this";

        // IHoverLabel is asked for a label without being told what is in the hand, so the
        // hand is remembered from CanInteract. PlayerHand.UpdateHover calls that (through
        // ActionTarget) before it asks for the label, and only while the crosshair is on
        // this bin, so the reference is this frame's. Null until the first hover.
        private PlayerHand _lastHand;

        public string HoverLabel => IsRefusing(_lastHand) ? refusedLabel : hoverLabel;

        // The bin destroys whatever it is handed, and a keeper going in is unrecoverable:
        // nothing spawns a second knife, so the run is left unable to cut bread. Refusing
        // in CanInteract rather than inside Interact also drives the outline and the
        // label, so the bin reads as "not now" instead of silently swallowing the knife.
        public bool CanInteract(PlayerHand hand)
        {
            _lastHand = hand;

            return hand != null && !hand.IsHandEmpty && !hand.HeldItem.IsKeeper;
        }

        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Trash);

            hand.DestroyHeldItem();
        }

        private bool IsRefusing(PlayerHand hand)
        {
            return hand != null && !hand.IsHandEmpty && hand.HeldItem.IsKeeper;
        }
    }
}
