// GameClient/Assets/Scripts/Interfaces/InteractionType.cs

/// <summary>
/// Defines the types of interactions an object can offer to the player.
/// This enum allows for a flexible, data-driven approach to designing encounters.
/// </summary>
public enum InteractionType
{
    Dialogue,       // Standard dialogue or LLM-generated commentary.
    Shop,           // Opens a shop UI for buying/selling items.
    QuestGiver,     // Offers or progresses a quest.
    SceneChange     // Triggers a transition to a new scene.
}
