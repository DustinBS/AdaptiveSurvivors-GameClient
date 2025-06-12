// GameClient/Assets/Scripts/Data/NPCData.cs

using UnityEngine;

/// <summary>
/// A ScriptableObject that holds the static data for a specific type of NPC.
/// This allows for creating and managing different NPC characters as assets in the project.
/// </summary>
[CreateAssetMenu(fileName = "NewNPCData", menuName = "Adaptive Survivors/NPC Data")]
public class NPCData : ScriptableObject
{
    [Header("NPC Details")]
    [Tooltip("The display name of the NPC.")]
    public string npcName = "Mysterious Stranger";

    [Tooltip("A description of the NPC's personality, used to prime the LLM for dialogue generation.")]
    [TextArea(2, 4)]
    public string npcPersonality = "A wise and ancient sage who speaks in riddles.";

    [Header("UI Visuals")]
    [Tooltip("The portrait sprite to display in the dialogue box. Can be left null to use a default.")]
    public Sprite portraitSprite;
}
