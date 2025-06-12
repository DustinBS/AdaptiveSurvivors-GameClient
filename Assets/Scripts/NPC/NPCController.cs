// GameClient/Assets/Scripts/NPC/NPCController.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls a single Non-Player Character (NPC) instance in the scene.
/// It uses an NPCScriptableObject for its core data and now holds a reference
/// to a DialogueData asset to kick off a conversation.
/// </summary>
public class NPCController : MonoBehaviour, IInteractable
{
    [Header("NPC Configuration")]
    [Tooltip("The ScriptableObject asset that defines this NPC's static properties like name and portrait.")]
    public NPCData npcData;

    [Header("Dialogue")]
    [Tooltip("The starting DialogueData asset for when the player interacts with this NPC.")]
    public DialogueData startingDialogue;

    // We no longer need the list of supported interactions here,
    // as that will now be driven by the choices within the DialogueData assets themselves.

    // Public properties to allow other systems (like the DialogueManager) to safely access the NPC's data.
    public string NPCName => npcData != null ? npcData.npcName : "Unknown";
    public string NPCPersonality => npcData != null ? npcData.npcPersonality : "A generic NPC.";
    public Sprite NPCPortrait => npcData != null ? npcData.portraitSprite : null;


    /// <summary>
    /// This method is called from the PlayerInteraction script when the player
    /// presses the 'E' key while in range.
    /// </summary>
    public void Interact()
    {
        if (startingDialogue == null)
        {
            Debug.LogError($"NPC '{NPCName}' has no starting dialogue assigned!", this);
            return;
        }

        // The NPC's job is simply to tell the DialogueManager to start a specific conversation.
        // This keeps the NPC completely decoupled from the UI and dialogue flow logic.
        DialogueManager.Instance.StartConversation(startingDialogue, this);
    }
}
