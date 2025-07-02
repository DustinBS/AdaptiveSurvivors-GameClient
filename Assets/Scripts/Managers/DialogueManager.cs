// GameClient/Assets/Scripts/Managers/DialogueManager.cs

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Linq;

/// <summary>
/// A stateful singleton manager that controls the interactive dialogue system.
/// It has been refactored to use a formal State Machine pattern for robust, crash-free execution.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    // Inner class for structuring the JSON payload to the cloud function.
    [System.Serializable]
    private class LLMRequestPayload
    {
        public string npc_personality;
        public KeyValuePair<string, object> statistic_to_comment_on;
    }

    public static DialogueManager Instance { get; private set; }

    private enum DialogueState
    {
        Inactive,
        DisplayingLine,
        LineFinished,
        AwaitingChoice,
        Ending,
        AwaitingLLM
    }

    [Header("Component References")]
    private DialogueUIController uiController;
    [SerializeField] private PlayerData playerData;

    [Header("Dialogue Settings")]
    [Tooltip("The delay in seconds after a line finishes naturally before the player can continue.")]
    [SerializeField] private float naturalEndDelay = 0.5f;
    [Tooltip("The speed of the typewriter effect in characters per second.")]
    [SerializeField] private float typewriterSpeed = 30f;

    [Header("LLM Settings")]
    [Tooltip("The full URL of the cloud function for post-run commentary.")]
    [SerializeField] private string cloudFunctionUrl;
    [SerializeField] private Sprite defaultPortrait;
    [Tooltip("A pool of phrases to display randomly while waiting for an LLM response.")]
    [SerializeField] private List<string> llmThinkingPhrases = new List<string> { "Hmm...", "Let me think...", "The ether speaks...", "Reading the echoes..." };
    [Tooltip("The probability (0-1) of an NPC commenting on a historical stat instead of the last run.")]
    [Range(0f, 1f)]
    [SerializeField] private float historicalStatChance = 0.2f;

    // --- State Management ---
    private string _currentLineFullText;
    private List<string> currentThinkingPhrases;

    private DialogueState currentState;
    private DialogueData currentConversation;
    private NPCController currentNpc;
    private int currentLineIndex;
    private Coroutine typewriterCoroutine;
    private PlayerControls playerControls;
    private bool acceptInput = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }

        currentState = DialogueState.Inactive;
    }

    void Start()
    {
        playerControls = PlayerInputManager.Instance.PlayerControls;

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
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (playerControls != null)
        {
            playerControls.UI.Submit.performed -= OnSubmitPerformed;
        }
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        uiController = FindFirstObjectByType<DialogueUIController>();

        if (uiController != null)
        {
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
        acceptInput = true;

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
                FinishLine();
                StartCoroutine(InputCooldown(true));
                break;
            case DialogueState.LineFinished:
                AdvanceConversation();
                break;
            case DialogueState.AwaitingLLM:
                DisplayNextThinkingPhrase();
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

        uiController.HideChoices();
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

        bool canCommentOnRun = RunSummaryService.IsNewSummaryAvailable;
        bool canCommentOnHistory = playerData != null && playerData.historicalStats.Count > 0;

        if (line.isLLMGenerated && (canCommentOnRun || canCommentOnHistory))
        {
            typewriterCoroutine = StartCoroutine(RequestLLMCommentary(line, speakerName));
        }
        else
        {
            _currentLineFullText = line.isLLMGenerated ? "(They look at you, but have nothing to say.)" : line.text;
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
        float charDelay = 1f / typewriterSpeed;
        if (charDelay <= 0) charDelay = 0.001f;

        foreach (char letter in text.ToCharArray())
        {
            if (currentState == DialogueState.Ending) yield break;
            currentText += letter;
            uiController.SetDialogueLine(speakerName, currentText);
            yield return new WaitForSeconds(charDelay);
        }

        if (currentState != DialogueState.Ending)
        {
           ShowPlayerResponses(currentConversation.lines[currentLineIndex]);
           StartCoroutine(InputCooldown(false));
        }
    }

    private IEnumerator RequestLLMCommentary(DialogueLine line, string speakerName)
    {
        // --- Fallback Logic ---
        if (string.IsNullOrEmpty(cloudFunctionUrl) || !cloudFunctionUrl.StartsWith("http"))
        {
            Debug.LogWarning("Cloud Function URL is not set or is invalid in DialogueManager. Using fallback dialogue.");
            _currentLineFullText = "(The heavens are silent today.)";
            yield return StartCoroutine(ShowTypewriterText(speakerName, _currentLineFullText));
            yield break; // Exit the coroutine early
        }

        currentState = DialogueState.AwaitingLLM;
        uiController.SetDialogueLine(speakerName, "");
        DisplayNextThinkingPhrase(true);

        // --- Weighted Topic Selection Logic ---
        Dictionary<string, object> chosenStatPool = null;
        
        bool hasRecentStats = RunSummaryService.IsNewSummaryAvailable && RunSummaryService.LastRunSummary.Count > 0;
        bool hasHistoricalStats = playerData.historicalStats.Count > 0;

        if (hasRecentStats && (!hasHistoricalStats || Random.value > historicalStatChance))
        {
            chosenStatPool = RunSummaryService.LastRunSummary;
            RunSummaryService.ConsumeSummary();
        }
        else if (hasHistoricalStats)
        {
            chosenStatPool = playerData.historicalStats.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }
        
        if (chosenStatPool == null || chosenStatPool.Count == 0)
        {
            _currentLineFullText = "(My mind is a blank... how unusual.)";
            yield return StartCoroutine(ShowTypewriterText(speakerName, _currentLineFullText));
            yield break;
        }

        // --- Select one random statistic from the chosen pool ---
        var randomStat = chosenStatPool.ElementAt(Random.Range(0, chosenStatPool.Count));

        // --- Prepare and Send Web Request ---
        var payload = new LLMRequestPayload
        {
            npc_personality = currentNpc.NPCPersonality,
            statistic_to_comment_on = randomStat
        };
        string jsonPayload = JsonConvert.SerializeObject(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(cloudFunctionUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                _currentLineFullText = request.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"Error requesting LLM commentary: {request.error}\n{request.downloadHandler.text}");
                _currentLineFullText = "(My thoughts are... clouded. Apologies.)";
            }
        }
        
        StartCoroutine(InputCooldown(customDuration: 0.5f));
        typewriterCoroutine = StartCoroutine(ShowTypewriterText(speakerName, _currentLineFullText));
    }
    
    private void DisplayNextThinkingPhrase(bool isFirstPhrase = false)
    {
        if (isFirstPhrase || currentThinkingPhrases == null || currentThinkingPhrases.Count == 0)
        {
            currentThinkingPhrases = new List<string>(llmThinkingPhrases);
        }

        int randomIndex = Random.Range(0, currentThinkingPhrases.Count);
        string phrase = currentThinkingPhrases[randomIndex];
        currentThinkingPhrases.RemoveAt(randomIndex);
        
        uiController.SetDialogueLine(currentNpc.NPCName, phrase);
    }

    private IEnumerator InputCooldown(bool isSkipped = false, float? customDuration = null)
    {
        acceptInput = false;
        if (customDuration.HasValue)
        {
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