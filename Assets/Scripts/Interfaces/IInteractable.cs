// GameClient/Assets/Scripts/Interfaces/IInteractable.cs

/// <summary>
/// Defines the contract for any object that the player can interact with.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Executes the interaction logic for this object.
    /// </summary>
    void Interact();

    /// <summary>
    /// Gets the UI text to display when this object is in range.
    /// </summary>
    /// <returns>A string representing the action, e.g., "Talk to Sage" or "Enter Portal".</returns>
    string GetInteractionPrompt();

    /// <summary>
    /// Define what kind of interaction this is
    /// </summary>
    InteractionType GetInteractionType();

}