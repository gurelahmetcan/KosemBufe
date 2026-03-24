/// <summary>
/// Contract for any object the player can click on and interact with.
/// Implemented by stations, holders, and any future interactable objects.
/// </summary>
public interface IInteractable
{
    /// <summary>Can the player interact right now given the current hand state?</summary>
    bool CanInteract(InHandController hand);

    /// <summary>Execute the interaction.</summary>
    void Interact(InHandController hand);
}
