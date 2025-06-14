// GameClient/Assets/Scripts/Interfaces/IInteractable.cs

/// <summary>
/// Defines a contract for any object that the player can interact with.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Executes the primary interaction logic for this object.
    /// </summary>
    void Interact();

    /// <summary>
    /// Returns the specific prompt text for this interaction (e.g., "Talk to Sage", "Enter Portal").
    /// </summary>
    /// <returns>A string representing the action.</returns>
    string GetInteractionPrompt();

    /// <summary>
    /// Gets the type of interaction. This can be used for more complex sorting or UI changes.
    /// </summary>
    /// <returns>The InteractionType enum value.</returns>
    InteractionType GetInteractionType();
}
