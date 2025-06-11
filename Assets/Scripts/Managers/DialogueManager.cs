// GameClient/Assets/Scripts/Managers/DialogueManager.cs

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking; // Required for UnityWebRequest

/// <summary>
/// A singleton manager that controls the entire dialogue UI and interaction flow.
/// It displays text, generates choice buttons, and handles LLM requests.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static DialogueManager Instance { get; private set; }

    [Header("UI Document")]
    [Tooltip("The UI Document GameObject that contains the Dialogue UI.")]
    [SerializeField] private UIDocument dialogueUIDocument;

    [Header("LLM Settings")]
    [Tooltip("Enable this to use placeholder text instead of making a real web request to the Cloud Function.")]
    [SerializeField] private bool useDebugMode = true;
    [Tooltip("The full URL of your Google Cloud Function for NPC commentary.")]
    [SerializeField] private string cloudFunctionUrl;

    // --- Private UI Element References ---
    private VisualElement dialogueContainer;
    private Label speakerLabel;
    private Label dialogueText;
    private VisualElement choiceButtonContainer;

    private Coroutine typewriterCoroutine;
    private NPCController currentNpc; // The NPC we are currently talking to

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Get references to the UXML elements
        var root = dialogueUIDocument.rootVisualElement;
        dialogueContainer = root.Q<VisualElement>("DialogueContainer");
        speakerLabel = root.Q<Label>("SpeakerLabel");
        dialogueText = root.Q<Label>("DialogueText");
        choiceButtonContainer = root.Q<VisualElement>("ChoiceButtonContainer");

        // Start with the dialogue UI hidden
        dialogueContainer.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Begins a dialogue with a specific NPC.
    /// </summary>
    /// <param name="npc">The NPC controller that the player is interacting with.</param>
    public void StartDialogue(NPCController npc)
    {
        currentNpc = npc;
        dialogueContainer.style.display = DisplayStyle.Flex; // Show the UI
        speakerLabel.text = npc.npcName;

        // Clear any previous dialogue and buttons
        dialogueText.text = "";
        choiceButtonContainer.Clear();

        // Dynamically create a button for each interaction the NPC supports
        foreach (var interactionType in npc.supportedInteractions)
        {
            Button choiceButton = new Button(() => OnInteractionChoice(interactionType));
            choiceButton.text = interactionType.ToString(); // "Chat", "Shop", etc.
            choiceButton.AddToClassList("dialogue-choice-button");
            choiceButtonContainer.Add(choiceButton);
        }
    }

    /// <summary>
    /// Handles the logic when a player clicks an interaction choice button.
    /// </summary>
    private void OnInteractionChoice(InteractionType choice)
    {
        // Clear the choice buttons as a decision has been made
        choiceButtonContainer.Clear();

        switch (choice)
        {
            case InteractionType.Chat:
                StartCoroutine(RequestLLMCommentary(currentNpc));
                break;
            case InteractionType.Shop:
                // Placeholder for shop logic
                DisplayLine("I have the finest wares in the land! (Shop UI not yet implemented).");
                StartCoroutine(EndDialogueAfterDelay(3f));
                break;
            case InteractionType.QuestGiver:
                // Placeholder for quest logic
                DisplayLine("I have a very important task for you... (Quest system not yet implemented).");
                StartCoroutine(EndDialogueAfterDelay(3f));
                break;
        }
    }

    /// <summary>
    /// Initiates the web request to the LLM Cloud Function.
    /// </summary>
    private IEnumerator RequestLLMCommentary(NPCController npc)
    {
        dialogueText.text = "Thinking...";

        if (useDebugMode)
        {
            yield return new WaitForSeconds(1f); // Simulate network delay
            DisplayLine($"This is debug commentary for {npc.npcName}, who has a {npc.npcPersonality} personality.");
            StartCoroutine(EndDialogueAfterDelay(4f));
            yield break; // Exit the coroutine
        }

        if (string.IsNullOrEmpty(cloudFunctionUrl))
        {
            DisplayLine("Error: Cloud Function URL is not configured.");
            StartCoroutine(EndDialogueAfterDelay(3f));
            yield break;
        }

        // Create the JSON payload for the request
        var requestData = new Dictionary<string, string>
        {
            { "player_id", "player_001" }, // This would be dynamic in a full game
            { "npc_personality", npc.npcPersonality }
        };
        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest webRequest = new UnityWebRequest(cloudFunctionUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                DisplayLine($"Error: {webRequest.error}");
            }
            else
            {
                var response = JsonUtility.FromJson<NpcCommentaryResponse>(webRequest.downloadHandler.text);
                DisplayLine(response.commentary);
            }
        }
        
        StartCoroutine(EndDialogueAfterDelay(5f));
    }

    /// <summary>
    /// Displays a line of text with a typewriter effect.
    /// </summary>
    public void DisplayLine(string line)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        typewriterCoroutine = StartCoroutine(ShowTypewriterText(line));
    }

    private IEnumerator ShowTypewriterText(string line)
    {
        dialogueText.text = "";
        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.03f); // Adjust for typing speed
        }
    }

    /// <summary>
    /// Coroutine to automatically end the dialogue after a set time.
    /// </summary>
    private IEnumerator EndDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndDialogue();
    }

    /// <summary>
    /// Hides the dialogue UI.
    /// </summary>
    public void EndDialogue()
    {
        dialogueContainer.style.display = DisplayStyle.None;
        currentNpc = null;
    }

    // A simple helper class to parse the JSON response from the Cloud Function
    [System.Serializable]
    private class NpcCommentaryResponse
    {
        public string commentary;
    }
}
