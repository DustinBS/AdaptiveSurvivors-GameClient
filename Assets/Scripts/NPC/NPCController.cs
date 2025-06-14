// GameClient/Assets/Scripts/NPC/NPCController.cs
using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [Tooltip("The ScriptableObject containing this NPC's data, like name and portrait.")]
    [SerializeField] private NPCData npcData;
    [Tooltip("The initial dialogue to trigger when the player interacts with this NPC.")]
    [SerializeField] private DialogueData initialDialogue;

    public void Interact()
    {
        // When interacted with, start the dialogue using the DialogueManager
        if (DialogueManager.Instance != null && initialDialogue != null)
        {
            // [FIX] Pass the NPCController instance itself to the DialogueManager
            DialogueManager.Instance.StartDialogue(initialDialogue);
        }
        else
        {
            // [FIX] Use npcName, which is the correct field in NPCData
            Debug.LogWarning($"DialogueManager instance not found or no initial dialogue set for {npcData.npcName}", this);
        }
    }

    /// <summary>
    /// Provides the dynamic interaction prompt text for this NPC.
    /// </summary>
    /// <returns>A formatted string with the NPC's name.</returns>
    public string GetInteractionPrompt()
    {
        // [FIX] Use npcName, which is the correct field in NPCData
        return $"Talk to {npcData.npcName}";
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Dialogue;
    }
}
