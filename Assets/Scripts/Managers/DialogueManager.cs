// GameClient/Assets/Scripts/Managers/DialogueManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueManager : MonoBehaviour
{
    // --- Public Events ---
    /// <summary>
    /// Event fired when the dialogue UI is opened or closed.
    /// Sends 'true' when dialogue starts (UI is active), 'false' when it ends.
    /// </summary>
    public static event Action<bool> OnDialogueStateChanged;

    // --- Public Singleton ---
    public static DialogueManager Instance { get; private set; }

    // --- Serialized Fields ---
    [Header("UI Setup")]
    [Tooltip("The UI Document component containing the dialogue UI.")]
    [SerializeField] private UIDocument dialogueUIDocument;

    [Header("Data References")]
    [Tooltip("Default PlayerData for portrait reference.")]
    [SerializeField] private PlayerData playerData;

    // --- Private UI Element References ---
    private VisualElement dialogueContainer;
    private VisualElement playerPortrait;
    private VisualElement npcPortrait;
    private Label speakerName;
    private Label dialogueText;
    private VisualElement continuePrompt;
    private VisualElement dialogueChoicesContainer;

    // --- State Management ---
    private Queue<string> currentDialogueLines;
    private DialogueData currentDialogueData;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typeWriterCoroutine;
    private string fullLine;


    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        currentDialogueLines = new Queue<string>();
    }

    private void Start()
    {
        if (dialogueUIDocument == null)
        {
            Debug.LogError("DialogueUIDocument is not assigned in the DialogueManager!");
            this.enabled = false;
            return;
        }
        InitializeUIReferences();
        dialogueContainer.style.display = DisplayStyle.None;
    }

    private void InitializeUIReferences()
    {
        var root = dialogueUIDocument.rootVisualElement;
        dialogueContainer = root.Q<VisualElement>("DialogueContainer");
        playerPortrait = root.Q<VisualElement>("PlayerPortrait");
        npcPortrait = root.Q<VisualElement>("NPCPortrait");
        speakerName = root.Q<Label>("SpeakerName");
        dialogueText = root.Q<Label>("DialogueText");
        continuePrompt = root.Q<VisualElement>("ContinuePrompt");
        dialogueChoicesContainer = root.Q<VisualElement>("DialogueChoicesContainer");

        if (playerData != null && playerData.portrait != null)
        {
             playerPortrait.style.backgroundImage = new StyleBackground(playerData.portrait);
        }
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        currentDialogueData = dialogueData;
        currentDialogueLines.Clear();

        foreach (var line in dialogueData.dialogueLines)
        {
            currentDialogueLines.Enqueue(line);
        }

        if (dialogueData.npcData != null && dialogueData.npcData.portrait != null)
        {
            npcPortrait.style.backgroundImage = new StyleBackground(dialogueData.npcData.portrait);
        }

        isDialogueActive = true;
        dialogueContainer.style.display = DisplayStyle.Flex;
        OnDialogueStateChanged?.Invoke(true); // Broadcast that dialogue has started

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // If the typewriter is running, the first press should skip it.
        if (isTyping)
        {
            SkipTypewriter();
            return;
        }

        if (currentDialogueLines.Count == 0)
        {
            EndDialogue();
            return;
        }

        fullLine = currentDialogueLines.Dequeue();
        UpdateSpeakerUI();

        if (typeWriterCoroutine != null)
        {
            StopCoroutine(typeWriterCoroutine);
        }
        typeWriterCoroutine = StartCoroutine(TypewriterEffect(fullLine));
    }

    private void UpdateSpeakerUI()
    {
        bool isPlayerSpeaking = (currentDialogueData.dialogueLines.Length - currentDialogueLines.Count) % 2 != 0;

        if (isPlayerSpeaking)
        {
            speakerName.text = playerData.characterName;
            playerPortrait.AddToClassList("active");
            npcPortrait.RemoveFromClassList("active");
        }
        else
        {
            speakerName.text = currentDialogueData.npcData.characterName;
            npcPortrait.AddToClassList("active");
            playerPortrait.RemoveFromClassList("active");
        }
    }

    private IEnumerator TypewriterEffect(string line)
    {
        dialogueText.text = "";
        continuePrompt.style.opacity = 0;
        isTyping = true;

        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.05f);
        }

        isTyping = false;
        continuePrompt.style.opacity = 1;
    }

    /// <summary>
    /// Finishes the typewriter effect instantly.
    /// </summary>
    private void SkipTypewriter()
    {
        if (isTyping)
        {
            StopCoroutine(typeWriterCoroutine);
            dialogueText.text = fullLine;
            isTyping = false;
            continuePrompt.style.opacity = 1;
        }
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        dialogueContainer.style.display = DisplayStyle.None;
        OnDialogueStateChanged?.Invoke(false); // Broadcast that dialogue has ended
    }

    private void Update()
    {
        // Using the new Input System's "anyKey" would be more robust,
        // but for now, we can check for a common key press.
        if (isDialogueActive && Input.anyKeyDown)
        {
            // We should prevent advancing dialogue if a UI button was clicked, for example.
            // This check can be made more robust later.
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                return;

            DisplayNextLine();
        }
    }
}
