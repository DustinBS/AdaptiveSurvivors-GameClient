// GameClient/Assets/Scripts/Managers/DialogueManager.cs

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Added for List<T>
using UnityEngine.SceneManagement;

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
    // registered at runtime
    private DialogueUIController uiController;
    [SerializeField] private PlayerData playerData;

    [Header("Dialogue Settings")]
    [Tooltip("The delay in seconds after a line finishes naturally before the player can continue.")]
    [SerializeField] private float naturalEndDelay = 0.5f;
    [Tooltip("The speed of the typewriter effect in characters per second.")]
    [SerializeField] private float typewriterSpeed = 30f;

    [Header("LLM Settings")]
    [SerializeField] private bool useDebugMode = true;
    [SerializeField] private string cloudFunctionUrl;
    [SerializeField] private Sprite defaultPortrait;
    [Tooltip("A pool of phrases to display randomly while waiting for an LLM response.")]
    [SerializeField] private List<string> llmThinkingPhrases = new List<string> { "Hmm...", "Let me think...", "Just a moment.", "..." };


    // --- State Management ---
    private string _currentLineFullText; // Caches the full text of the line currently being displayed.

    private bool isWaitingForLLM = false; // Flag to track if we're in the LLM waiting state.
    private List<string> currentThinkingPhrases; // The temporary pool of phrases for the current request.

    private DialogueState currentState;
    private DialogueData currentConversation;
    private NPCController currentNpc;
    private int currentLineIndex;
    private Coroutine typewriterCoroutine;
    private PlayerControls playerControls;
    private bool acceptInput = true; // NEW: Flag to control input processing

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }

        currentState = DialogueState.Inactive;
    }

    void Start()
    {
        // 1. Safely get the controls instance.
        playerControls = PlayerInputManager.Instance.PlayerControls;

        // 2. Immediately subscribe to the necessary events.
        if (playerControls != null)
        {
            playerControls.UI.Submit.performed += OnSubmitPerformed;
        }
        else
        {
            Debug.LogError("DialogueManager: PlayerControls could not be found in Start(). Dialogue input will not work.");
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Unsubscribe from both sceneLoaded and player input to prevent memory leaks.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (playerControls != null)
        {
            playerControls.UI.Submit.performed -= OnSubmitPerformed;
        }
    }

    // This method runs every time a new scene is loaded.
    // It mirrors the logic from PlayerInteraction.cs script.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the UI controller in the newly loaded scene.
        uiController = FindObjectOfType<DialogueUIController>();

        if (uiController != null)
        {
            // If we found one, subscribe to its event.
            uiController.OnChoiceSelected += OnPlayerResponseClicked;
        }
    }

    public void StartConversation(DialogueData dialogue, NPCController npc)
    {
        if (currentState != DialogueState.Inactive || uiController == null) return;
        if (dialogue == null || dialogue.lines.Count == 0 || uiController == null) return;

        PlayerInputManager.Instance.SwitchToUIControls();
        currentConversation = dialogue;
        currentNpc = npc;
        currentLineIndex = -1;
        acceptInput = true; // Ensure input is accepted at the start

        uiController.UpdatePortraits(playerData.characterData.characterPortrait, npc.NPCPortrait ?? defaultPortrait);
        uiController.ShowDialogue(true);

        AdvanceConversation();
    }

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        switch (currentState)
        {
            case DialogueState.DisplayingLine:
                // If we are waiting for the LLM, show the next thinking phrase.
                if (isWaitingForLLM)
                {
                    DisplayNextThinkingPhrase();
                }
                // Otherwise, perform the normal line skip.
                else
                {
                    FinishLine();
                    StartCoroutine(InputCooldown(true));
                }
                break;
            case DialogueState.LineFinished:
                AdvanceConversation();
                break;
        }
    }

    private void AdvanceConversation()
    {
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
            speakerName = playerData.characterData.characterName;
        }
        else
        {
            uiController.SetActiveSpeaker(DialogueUIController.PortraitSide.NPC);
            speakerName = currentNpc.NPCName;
        }

        _currentLineFullText = line.text;

        uiController.HideChoices();
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

        if (line.isLLMGenerated)
        {
            typewriterCoroutine = StartCoroutine(RequestLLMCommentary(line, speakerName));
        }
        else
        {
            typewriterCoroutine = StartCoroutine(ShowTypewriterText(speakerName, _currentLineFullText));
        }
    }

    private void FinishLine()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (currentState == DialogueState.Ending || currentConversation == null) return;

        var currentLine = currentConversation.lines[currentLineIndex];
        uiController.SetDialogueLine(
            currentLine.speaker == DialogueLine.Speaker.Player ? playerData.characterData.characterName : currentNpc.NPCName,
            _currentLineFullText
        );

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
        if (currentState != DialogueState.AwaitingChoice || uiController == null)
        {
            return;
        }

        uiController.HideChoices();
        if (response.nextDialogue != null)
        {
            currentConversation = response.nextDialogue;
            currentLineIndex = -1; AdvanceConversation();
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
        // MODIFIED: Use the typewriterSpeed variable for character delay calculation
        float charDelay = 1f / typewriterSpeed;
        if (charDelay <= 0) charDelay = 0.001f; // Prevent division by zero

        foreach (char letter in text.ToCharArray())
        {
            if (currentState == DialogueState.Ending) yield break;
            currentText += letter;
            uiController.SetDialogueLine(speakerName, currentText);
            yield return new WaitForSeconds(charDelay);
        }

        if (currentState != DialogueState.Ending)
        {
           // The line finished displaying naturally.
           ShowPlayerResponses(currentConversation.lines[currentLineIndex]);
           // MODIFIED: Start the cooldown to prevent accidental skipping.
           StartCoroutine(InputCooldown(false));
        }
    }

    private IEnumerator RequestLLMCommentary(DialogueLine line, string speakerName)
    {
        currentState = DialogueState.DisplayingLine;
        isWaitingForLLM = true; // Set the flag
        uiController.ShowContinuePrompt(false);

        // Initialize the thinking phrases for this request.
        DisplayNextThinkingPhrase(true);

        string generatedText;

        // --- This is where your actual web request would go ---
        if (useDebugMode)
        {
            // Simulate a long network delay for testing.
            yield return new WaitForSeconds(2.0f);
            generatedText = "This is a dynamically generated response after a long wait!";
        }
        else
        {
            // Here you would implement your async UnityWebRequest logic.
            // For now, we'll just use a placeholder.
            generatedText = "This should be replaced by your real web request result.";
            // Example:
            // var request = UnityWebRequest.Post(cloudFunctionUrl, "{}");
            // yield return request.SendWebRequest();
            // if(request.result == UnityWebRequest.Result.Success) {
            //    generatedText = request.downloadHandler.text;
            // } else {
            //    generatedText = "Sorry, I'm having trouble thinking right now.";
            // }
        }
        _currentLineFullText = generatedText;

        isWaitingForLLM = false;

        if (currentState != DialogueState.Ending)
        {
            // This prevents the user's spam-clicking from skipping the result.
            StartCoroutine(InputCooldown(customDuration: 1.0f));

            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(ShowTypewriterText(speakerName, _currentLineFullText));
        }
    }


    // A helper method to manage and display the thinking phrases.
    private void DisplayNextThinkingPhrase(bool isFirstPhrase = false)
    {
        // If this is the first phrase or our temporary pool is empty, refill it.
        if (isFirstPhrase || currentThinkingPhrases == null || currentThinkingPhrases.Count == 0)
        {
            // Make a copy from the master list so we can safely remove items.
            currentThinkingPhrases = new List<string>(llmThinkingPhrases);
        }

        // Pick a random phrase from the current pool.
        int randomIndex = Random.Range(0, currentThinkingPhrases.Count);
        string phrase = currentThinkingPhrases[randomIndex];

        // Remove it so it's not picked again until the pool is refilled.
        currentThinkingPhrases.RemoveAt(randomIndex);

        // Display the thinking phrase.
        uiController.SetDialogueLine(currentNpc.NPCName, phrase);
    }

    // NEW: Coroutine to manage input cooldown.
    /// <summary>
    /// Prevents input for a short duration to avoid accidental skips.
    /// </summary>
    /// <param name="isSkipped">If true, uses a minimal delay. If false, uses the configured natural end delay.</param>
    private IEnumerator InputCooldown(bool isSkipped = false, float? customDuration = null)
    {
        acceptInput = false;

        if (customDuration.HasValue)
        {
            // If a custom duration is provided, use it.
            yield return new WaitForSeconds(customDuration.Value);
        }
        else if (isSkipped)
        {
            yield return new WaitForEndOfFrame();
        }
        else
        {
            yield return new WaitForSeconds(naturalEndDelay);
        }

        acceptInput = true;
    }

    public void EndConversation()
    {
        if (currentState == DialogueState.Inactive) return;

        currentState = DialogueState.Ending;
        // MODIFIED: Safely stop the coroutine if it's running
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

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