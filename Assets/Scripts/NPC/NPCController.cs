// GameClient/Assets/Scripts/NPC/NPCController.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls the behavior of a single Non-Player Character (NPC).
/// It implements the IInteractable interface, allowing the player to initiate dialogue.
/// This component holds all data relevant to the NPC and their available interactions.
/// </summary>
public class NPCController : MonoBehaviour, IInteractable
{
    [Header("NPC Details")]
    [Tooltip("The display name of the NPC.")]
    public string npcName = "Mysterious Stranger";

    [Tooltip("A description of the NPC's personality, used to prime the LLM for dialogue generation.")]
    [TextArea(2, 4)]
    public string npcPersonality = "A wise and ancient sage who speaks in riddles.";

    [Header("Supported Interactions")]
    [Tooltip("The list of interaction types this NPC offers to the player.")]
    public List<InteractionType> supportedInteractions = new List<InteractionType> { InteractionType.Chat };

    // This property allows other systems, like a UI prompt, to know what to display.
    public string InteractionPrompt => $"Talk to {npcName}";

    /// <summary>
    /// This method is called from the PlayerInteraction script when the player
    /// presses the 'E' key while in range.
    /// </summary>
    public void Interact()
    {
        Debug.Log($"Player interacted with {npcName}.");

        // The NPC's only job is to tell the DialogueManager to start a conversation,
        // passing itself as the context. This keeps the NPC decoupled from the UI system.
        DialogueManager.Instance.StartDialogue(this);
    }

    // To make this work, the NPC GameObject should have a Collider2D component
    // so the PlayerInteraction script's trigger can detect it.
}
