// GameClient/Assets/Scripts/Managers/DialogueManager.cs

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

/// <summary>
/// A stateful singleton manager that controls the entire interactive dialogue system.
/// It drives the two-portrait UI, handles branching conversations from DialogueData,
/// processes player choices, and manages LLM requests.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI & Data References")]
    [SerializeField] private UIDocument dialogueUIDocument;
    [SerializeField] private PlayerData playerData; // Reference to player's data for portrait/name

    [Header("LLM Settings")]
    [SerializeField] private bool useDebugMode = true;
    [SerializeField] private string cloudFunctionUrl;
    [SerializeField] private Sprite defaultPortrait;

    // --- UI Element References ---
    private VisualElement dialogueContainer;
    private VisualElement playerPortraitElement;
    private VisualElement npcPortraitElement;
    private Label playerSpeakerLabel;
    private Label npcSpeakerLabel;
    private Label dialogueText;
    private VisualElement choiceButtonContainer;

    // --- State Management ---
    private DialogueData currentConversation;
    private NPCController currentNpc;
    private int currentLineIndex;
    private bool isDisplayingLine = false;
    private Coroutine typewriterCoroutine;

    private PlayerControls playerControls;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }

        // --- Get PlayerControls from the central manager ---
        playerControls = PlayerInputManager.Instance.PlayerControls;

        var root = dialogueUIDocument.rootVisualElement;
        dialogueContainer = root.Q<VisualElement>("DialogueContainer");
        playerPortraitElement = root.Q<VisualElement>("PlayerPortrait");
        npcPortraitElement = root.Q<VisualElement>("NPCPortrait");
        playerSpeakerLabel = root.Q<Label>("PlayerSpeakerLabel");
        npcSpeakerLabel = root.Q<Label>("NPCSpeakerLabel");
        dialogueText = root.Q<Label>("DialogueText");
        choiceButtonContainer = root.Q<VisualElement>("ChoiceButtonContainer");

        dialogueContainer.style.display = DisplayStyle.None;
    }

    void OnEnable()
    {
        playerControls.UI.Submit.performed += OnSubmitPerformed;
    }

    void OnDisable()
    {
        playerControls.UI.Submit.performed -= OnSubmitPerformed;
    }

    /// <summary>
    /// Starts a new conversation using a DialogueData asset.
    /// </summary>
    public void StartConversation(DialogueData dialogue, NPCController npc)
    {
        if (dialogue == null || dialogue.lines.Count == 0) return;

        // --- Disable Player controls and enable UI controls ---
        PlayerInputManager.Instance.SwitchToUIControls();
        dialogueContainer.style.display = DisplayStyle.Flex;
        currentConversation = dialogue;
        currentNpc = npc;
        currentLineIndex = -1;

        AdvanceConversation();
    }

    /// <summary>
    /// Called when the player presses the submit button during a conversation.
    /// </summary>
    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (currentConversation != null)
        {
            AdvanceConversation();
        }
    }

    /// <summary>
    /// Moves the conversation to the next line or handles choices.
    /// </summary>
    private void AdvanceConversation()
    {
        if (isDisplayingLine)
        {
            FinishLine();
            return;
        }

        if (choiceButtonContainer.childCount > 0)
        {
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentConversation.lines.Count)
        {
            EndConversation();
            return;
        }

        DisplayLine(currentConversation.lines[currentLineIndex]);
    }

    /// <summary>
    /// Displays a single line of dialogue and updates the UI.
    /// </summary>
    private void DisplayLine(DialogueLine line)
    {
        UpdateSpeakerUI(line.speaker, line.speakerData);
        choiceButtonContainer.Clear();

        if (line.isLLMGenerated)
        {
            StartCoroutine(RequestLLMCommentary(line));
        }
        else
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(ShowTypewriterText(line.text));
        }
    }

    /// <summary>
    /// Instantly finishes the typewriter effect for the current line.
    /// </summary>
    private void FinishLine()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            isDisplayingLine = false;
            DialogueLine currentLine = currentConversation.lines[currentLineIndex];
            dialogueText.text = currentLine.text;
            ShowPlayerResponses(currentLine);
        }
    }

    /// <summary>
    /// Updates the UI portraits and speaker labels.
    /// </summary>
    private void UpdateSpeakerUI(DialogueLine.Speaker activeSpeaker, NPCData npcData)
    {
        playerPortraitElement.style.backgroundImage = new StyleBackground(playerData.playerPortrait);
        npcPortraitElement.style.backgroundImage = new StyleBackground(npcData?.portraitSprite ?? defaultPortrait);

        playerSpeakerLabel.text = playerData.playerName;
        npcSpeakerLabel.text = npcData?.npcName ?? "Unknown";

        if (activeSpeaker == DialogueLine.Speaker.Player)
        {
            playerSpeakerLabel.style.display = DisplayStyle.Flex;
            npcSpeakerLabel.style.display = DisplayStyle.None;
            playerPortraitElement.RemoveFromClassList("inactive-speaker");
            npcPortraitElement.AddToClassList("inactive-speaker");
        }
        else
        {
            playerSpeakerLabel.style.display = DisplayStyle.None;
            npcSpeakerLabel.style.display = DisplayStyle.Flex;
            playerPortraitElement.AddToClassList("inactive-speaker");
            npcPortraitElement.RemoveFromClassList("inactive-speaker");
        }
    }

    /// <summary>
    /// Displays the player choice buttons for a given line.
    /// </summary>
    private void ShowPlayerResponses(DialogueLine line)
    {
        choiceButtonContainer.Clear();
        if (line.playerResponses != null && line.playerResponses.Count > 0)
        {
            foreach (var response in line.playerResponses)
            {
                Button choiceButton = new Button(() => OnPlayerResponseClicked(response));
                choiceButton.text = response.responseText;
                choiceButton.AddToClassList("dialogue-choice-button");
                choiceButtonContainer.Add(choiceButton);
            }
        }
    }

    /// <summary>
    /// Called when a player clicks a response button.
    /// </summary>
    private void OnPlayerResponseClicked(PlayerResponse response)
    {
        choiceButtonContainer.Clear();
        if (response.nextDialogue != null)
        {
            currentConversation = response.nextDialogue;
            currentLineIndex = -1;
            AdvanceConversation();
        }
        else
        {
            EndConversation();
        }
    }

    /// <summary>
    /// Coroutine for the typewriter text effect.
    /// </summary>
    private IEnumerator ShowTypewriterText(string text)
    {
        isDisplayingLine = true;
        dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.03f);
        }
        isDisplayingLine = false;
        ShowPlayerResponses(currentConversation.lines[currentLineIndex]);
    }

    private IEnumerator RequestLLMCommentary(DialogueLine line)
    {
        isDisplayingLine = true;
        dialogueText.text = "Hmmm...";
        string generatedText = "This is default LLM debug text. The real call would go here.";

        if (useDebugMode)
        {
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            // Web Request Logic...
        }

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(ShowTypewriterText(generatedText));
    }

    /// <summary>
    /// Ends the current conversation and hides the UI.
    /// </summary>
    public void EndConversation()
    {
        dialogueContainer.style.display = DisplayStyle.None;
        currentConversation = null;
        currentNpc = null;

        // --- Disable UI controls and re-enable Player controls ---
        PlayerInputManager.Instance.SwitchToPlayerControls();
    }

    [System.Serializable]
    private class NpcCommentaryResponse { public string commentary; }
}
