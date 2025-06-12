// GameClient/Assets/Scripts/Managers/DialogueManager.cs

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// A singleton manager that controls the entire dialogue UI and interaction flow.
/// It displays text, generates choice buttons, and handles LLM requests.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Document")]
    [SerializeField] private UIDocument dialogueUIDocument;

    [Header("LLM Settings")]
    [SerializeField] private bool useDebugMode = true;
    [SerializeField] private string cloudFunctionUrl;
    [SerializeField] private Sprite defaultPortrait; // A default '?' sprite if an NPC has no portrait

    // UI Element References
    private VisualElement dialogueContainer;
    private VisualElement portraitElement;
    private Label speakerLabel;
    private Label dialogueText;
    private VisualElement choiceButtonContainer;

    private Coroutine typewriterCoroutine;
    private NPCController currentNpc;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }

        var root = dialogueUIDocument.rootVisualElement;
        dialogueContainer = root.Q<VisualElement>("DialogueContainer");
        portraitElement = root.Q<VisualElement>("Portrait");
        speakerLabel = root.Q<Label>("SpeakerLabel");
        dialogueText = root.Q<Label>("DialogueText");
        choiceButtonContainer = root.Q<VisualElement>("ChoiceButtonContainer");

        dialogueContainer.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Begins a dialogue, setting up the UI with the NPC's data.
    /// </summary>
    public void StartDialogue(NPCController npc)
    {
        currentNpc = npc;
        dialogueContainer.style.display = DisplayStyle.Flex;

        // --- MODIFIED: Use public properties from NPCController ---
        speakerLabel.text = npc.NPCName;

        // --- NEW: Set the portrait ---
        Sprite portrait = npc.NPCPortrait != null ? npc.NPCPortrait : defaultPortrait;
        Debug.Log($"Setting portrait for {npc.NPCName}: {portrait.name}");
        portraitElement.style.backgroundImage = new StyleBackground(portrait);

        dialogueText.text = "";
        choiceButtonContainer.Clear();

        foreach (var interactionType in npc.supportedInteractions)
        {
            Button choiceButton = new Button(() => OnInteractionChoice(interactionType))
            {
                text = interactionType.ToString()
            };
            choiceButton.AddToClassList("dialogue-choice-button");
            choiceButtonContainer.Add(choiceButton);
        }
    }

    private void OnInteractionChoice(InteractionType choice)
    {
        choiceButtonContainer.Clear();
        switch (choice)
        {
            case InteractionType.Chat:
                StartCoroutine(RequestLLMCommentary(currentNpc));
                break;
            case InteractionType.Shop:
                DisplayLine("I have the finest wares... if you have the coin. (Shop Not Implemented)");
                StartCoroutine(EndDialogueAfterDelay(3f));
                break;
            case InteractionType.QuestGiver:
                DisplayLine("A hero is needed... (Quests Not Implemented)");
                StartCoroutine(EndDialogueAfterDelay(3f));
                break;
        }
    }

    /// <summary>
    /// Displays a line of text with a typewriter effect.
    /// </summary>
    public void DisplayLine(string line)
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(ShowTypewriterText(line));
    }

    private IEnumerator ShowTypewriterText(string line)
    {
        dialogueText.text = "";
        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.03f);
        }
    }

    private IEnumerator RequestLLMCommentary(NPCController npc)
    {
        dialogueText.text = "Hmmm...";

        if (useDebugMode)
        {
            yield return new WaitForSeconds(1.5f);
            // --- MODIFIED: Use public properties ---
            DisplayLine($"This is debug commentary for {npc.NPCName}, who has a '{npc.NPCPersonality}' personality.");
            StartCoroutine(EndDialogueAfterDelay(4f));
            yield break;
        }

        // --- (Web request logic remains the same, but now uses npc.NPCPersonality) ---
        var requestData = new Dictionary<string, string>
        {
            { "player_id", "player_001" },
            { "npc_personality", npc.NPCPersonality }
        };
        // (Rest of the web request code)
    }

    private IEnumerator EndDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndDialogue();
    }

    public void EndDialogue()
    {
        dialogueContainer.style.display = DisplayStyle.None;
        currentNpc = null;
    }

    [System.Serializable]
    private class NpcCommentaryResponse { public string commentary; }
}
