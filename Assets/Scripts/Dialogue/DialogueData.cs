// GameClient/Assets/Scripts/Dialogue/DialogueData.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that represents an entire conversation or a piece of one.
/// It contains a series of dialogue lines and can branch into player responses.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Adaptive Survivors/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Tooltip("The series of dialogue lines that make up this conversation.")]
    public List<DialogueLine> lines;
}

/// <summary>
/// Represents a single line of dialogue in a conversation.
/// </summary>
[System.Serializable]
public class DialogueLine
{
    // An enum to define who is speaking this line.
    public enum Speaker { Player, NPC }

    [Tooltip("Who is speaking this line? The Player or the NPC?")]
    public Speaker speaker;

    [Tooltip("The character data for the speaker of this line. Leave null if the Player is speaking.")]
    public NPCData speakerData;

    [Tooltip("The text content of the dialogue line. This will be ignored if isLLMGenerated is true.")]
    [TextArea(3, 10)]
    public string text;

    [Header("LLM Generation")]
    [Tooltip("If checked, the DialogueManager will make a web request to the LLM to generate this line's text instead of using the text field above.")]
    public bool isLLMGenerated = false;

    [Header("Player Choices")]
    [Tooltip("A list of choices the player can make after this line is delivered. If empty, the dialogue proceeds to the next line automatically on interaction.")]
    public List<PlayerResponse> playerResponses;
}

/// <summary>
/// Represents a single choice the player can make in response to a dialogue line.
/// </summary>
[System.Serializable]
public class PlayerResponse
{
    [Tooltip("The text that will appear on the player's choice button.")]
    public string responseText;

    [Tooltip("The DialogueData asset to jump to if the player selects this response. This enables branching conversations.")]
    public DialogueData nextDialogue;
}
