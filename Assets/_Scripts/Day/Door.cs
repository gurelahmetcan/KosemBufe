using UnityEngine;

namespace Bufe
{
    public class Door : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [Header("References")]
        [Tooltip("Controls the day cycle.")]
        public DayManager dayManager;

        [Header("Hover Labels")]
        [Tooltip("Label when the day can end.")]
        [SerializeField] private string readyLabel = "Go Home";

        [Tooltip("Label while the day is running.")]
        [SerializeField] private string notReadyLabel = "It's not time to close yet.";

        public string HoverLabel => CanInteract(null) ? readyLabel : notReadyLabel;

        public bool CanInteract(PlayerHand hand)
        {
            return dayManager != null && dayManager.CanEndDay;
        }

        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;
            Log.Info("[Door] Day ended. Going home.");
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Door);

            dayManager.EndDay();
        }
    }
}
