/// <summary>
/// Contract for any object in the new assembly system that the player
/// can interact with (PrepStation, new Grill, new Toaster, etc.).
///
/// Uses PlayerHand instead of the legacy InHandController.
/// </summary>
public interface IAssemblyInteractable
{
    /// <summary>Can the player interact right now given the current hand state?</summary>
    bool CanInteract(PlayerHand hand);

    /// <summary>Execute the interaction.</summary>
    void Interact(PlayerHand hand);
}
