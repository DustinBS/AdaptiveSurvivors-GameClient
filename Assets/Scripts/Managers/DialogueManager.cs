// GameClient/Assets/Scripts/Managers/DialogueManager.cs

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// A stateful singleton manager that controls the interactive dialogue system.
/// It has been refactored to use a formal State Machine pattern for robust, crash-free execution.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // Defines the possible states of the dialogue system.
    private enum DialogueState
    {
        Inactive,           // Not in a conversation.
        DisplayingLine,     // Typewriter effect is running.
        LineFinished,       // Line is fully displayed, waiting for input to continue.
        AwaitingChoice,     // Player choice buttons are visible.
        Ending              // Conversation is wrapping up.
    }

    [Header("Component References")]
    [Tooltip("The controller for the dialogue UI. Drag the GameObject with the DialogueUIController script here.")]
    [SerializeField] private DialogueUIController uiController;
    [SerializeField] private PlayerData playerData;

    [Header("LLM Settings")]
    [SerializeField] private bool useDebugMode = true;
    [SerializeField] private string cloudFunctionUrl;
    [SerializeField] private Sprite defaultPortrait;

    // --- State Management ---
    private DialogueState currentState;
    private DialogueData currentConversation;
    private NPCController currentNpc;
    private int currentLineIndex;
    private Coroutine typewriterCoroutine;
    private PlayerControls playerControls;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }

        playerControls = PlayerInputManager.Instance.PlayerControls;
        currentState = DialogueState.Inactive;
    }

    void OnEnable()
    {
        playerControls.UI.Submit.performed += OnSubmitPerformed;
        if (uiController != null)
        {
            uiController.OnChoiceSelected += OnPlayerResponseClicked;
        }
    }

    void OnDisable()
    {
        playerControls.UI.Submit.performed -= OnSubmitPerformed;
        if (uiController != null)
        {
            uiController.OnChoiceSelected -= OnPlayerResponseClicked;
        }
    }

    public void StartConversation(DialogueData dialogue, NPCController npc)
    {
        if (currentState != DialogueState.Inactive) return;
        if (dialogue == null || dialogue.lines.Count == 0 || uiController == null) return;

        PlayerInputManager.Instance.SwitchToUIControls();
        currentConversation = dialogue;
        currentNpc = npc;
        currentLineIndex = -1;

        uiController.UpdatePortraits(playerData.playerPortrait, npc.NPCPortrait ?? defaultPortrait);
        uiController.ShowDialogue(true);

        AdvanceConversation();
    }

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        // Only process submit input based on the current state.
        switch (currentState)
        {
            case DialogueState.DisplayingLine:
                FinishLine();
                break;
            case DialogueState.LineFinished:
                AdvanceConversation();
                break;
        }
    }

    private void AdvanceConversation()
    {
        // Check if we are at the end of the conversation.
        currentLineIndex++;
        if (currentLineIndex >= currentConversation.lines.Count)
        {
            EndConversation();
            return;
        }
        DisplayLine(currentConversation.lines[currentLineIndex]);
    }

    private void DisplayLine(DialogueLine line)
    {
        string speakerName;
        if (line.speaker == DialogueLine.Speaker.Player)
        {
            uiController.SetActiveSpeaker(DialogueUIController.PortraitSide.Player);
            speakerName = playerData.playerName;
        }
        else
        {
            uiController.SetActiveSpeaker(DialogueUIController.PortraitSide.NPC);
            speakerName = currentNpc.NPCName;
        }

        uiController.HideChoices();

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

        if (line.isLLMGenerated)
        {
            typewriterCoroutine = StartCoroutine(RequestLLMCommentary(line, speakerName));
        }
        else
        {
            typewriterCoroutine = StartCoroutine(ShowTypewriterText(speakerName, line.text));
        }
    }

    private void FinishLine()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        if (currentState == DialogueState.Ending || currentConversation == null) return;

        var currentLine = currentConversation.lines[currentLineIndex];
        uiController.SetDialogueLine(currentLine.speaker == DialogueLine.Speaker.Player ? playerData.playerName : currentNpc.NPCName, currentLine.text);

        ShowPlayerResponses(currentLine);
    }

    private void ShowPlayerResponses(DialogueLine line)
    {
        if (line.playerResponses != null && line.playerResponses.Count > 0)
        {
            currentState = DialogueState.AwaitingChoice;
            uiController.ShowContinuePrompt(false);
            uiController.DisplayChoices(line.playerResponses);
        }
        else
        {
            currentState = DialogueState.LineFinished;
            uiController.ShowContinuePrompt(true);
        }
    }

    private void OnPlayerResponseClicked(PlayerResponse response)
    {
        if (currentState != DialogueState.AwaitingChoice) return;

        uiController.HideChoices();

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

    private IEnumerator ShowTypewriterText(string speakerName, string text)
    {
        currentState = DialogueState.DisplayingLine;
        uiController.ShowContinuePrompt(false);
        uiController.SetDialogueLine(speakerName, "");

        string currentText = "";
        foreach (char letter in text.ToCharArray())
        {
            if (currentState == DialogueState.Ending) yield break;
            currentText += letter;
            uiController.SetDialogueLine(speakerName, currentText);
            yield return new WaitForSeconds(0.03f);
        }

        if (currentState != DialogueState.Ending)
        {
           ShowPlayerResponses(currentConversation.lines[currentLineIndex]);
        }
    }

    private IEnumerator RequestLLMCommentary(DialogueLine line, string speakerName)
    {
        currentState = DialogueState.DisplayingLine;
        uiController.ShowContinuePrompt(false);
        uiController.SetDialogueLine(speakerName, "Hmmm...");
        string generatedText = "This is default LLM debug text. The real call would go here.";

        if (useDebugMode)
        {
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            // Full Web Request Logic would go here...
        }

        if (currentState != DialogueState.Ending)
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(ShowTypewriterText(speakerName, generatedText));
        }
    }

    public void EndConversation()
    {
        if (currentState == DialogueState.Inactive) return;

        currentState = DialogueState.Ending;

        if (uiController != null)
        {
            uiController.ShowDialogue(false);
        }

        currentConversation = null;
        currentNpc = null;

        PlayerInputManager.Instance.SwitchToPlayerControls();

        currentState = DialogueState.Inactive;
    }
}
