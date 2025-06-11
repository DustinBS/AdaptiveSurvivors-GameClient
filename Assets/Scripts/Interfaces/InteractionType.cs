// GameClient/Assets/Scripts/Interfaces/InteractionType.cs

/// <summary>
/// Defines the types of interactions an NPC can offer to the player.
/// This enum allows for a flexible, data-driven approach to designing NPC encounters.
/// </summary>
public enum InteractionType
{
    Chat,       // Standard dialogue or LLM-generated commentary.
    Shop,       // Opens a shop UI for buying/selling items.
    QuestGiver  // Offers or progresses a quest.
}
