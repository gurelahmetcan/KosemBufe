using Bufe;
using UnityEngine;

public class Door : MonoBehaviour, IAssemblyInteractable, IHoverLabel
{
    [Header("References")]
    [Tooltip("DayManager that controls the day cycle.")]
    public DayManager dayManager;

    [Header("Hover Labels")]
    [Tooltip("Shown when the day can be ended (player may leave).")]
    [SerializeField] private string readyLabel = "Go Home";

    [Tooltip("Shown while the day is still running and the door can't be used yet.")]
    [SerializeField] private string notReadyLabel = "It's not time to close yet.";

    public string HoverLabel => CanInteract(null) ? readyLabel : notReadyLabel;

    public bool CanInteract(PlayerHand hand)
    {
        return dayManager != null && dayManager.CanEndDay;
    }

    public void Interact(PlayerHand hand)
    {
        if (!CanInteract(hand)) return;
        Debug.Log("[Door] Day ended. Going home.");
        if(AudioManager.Instance)
            AudioManager.Instance.PlaySfx(Sfx.Door);

        dayManager.EndDay();
    }
}
