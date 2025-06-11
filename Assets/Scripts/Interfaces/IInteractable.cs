// GameClient/Assets/Scripts/Interfaces/IInteractable.cs

/// <summary>
/// Defines a contract for any object that the player can interact with.
/// GameObjects like NPCs, portals, chests, or quest items will implement this interface.
/// This ensures that the PlayerInteraction script can talk to any of them
/// through a common method, promoting a clean and decoupled architecture.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// This method is called by the PlayerInteraction script when the player
    /// presses the interact key while near an object implementing this interface.
    /// </summary>
    void Interact();
}