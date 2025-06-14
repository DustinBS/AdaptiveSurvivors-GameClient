// GameClient/Assets/Scripts/Managers/DialogueManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueManager : MonoBehaviour
{
    // --- Public Events ---
    public static event Action<bool> OnDialogueStateChanged;

    // --- Public Singleton ---
    public static DialogueManager Instance { get; private set; }

    // --- Serialized Fields ---
    [Header("UI Setup")]
    [SerializeField] private UIDocument dialogueUIDocument;

    [Header("Data References")]
    [Tooltip("Reference to the main PlayerData asset.")]
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
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }
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
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        // [FIX] Ensure there is CharacterData selected before starting a dialogue
        if (playerData.characterData == null)
        {
            Debug.LogError("Cannot start dialogue, PlayerData has no CharacterData assigned!");
            return;
        }

        // [FIX] The original DialogueData script uses 'lines' not 'dialogueLines'
        if (dialogueData == null || dialogueData.lines.Count == 0) return;

        currentDialogueData = dialogueData;
        currentDialogueLines.Clear();

        // [FIX] The original DialogueData structure is more complex; this is a simplified version for now.
        // We will adapt this later if we re-implement branching dialogue.
        foreach (var line in dialogueData.lines)
        {
            currentDialogueLines.Enqueue(line.text);
        }

        // [FIX] Access the portrait from the CharacterData sub-asset
        playerPortrait.style.backgroundImage = new StyleBackground(playerData.characterData.characterPortrait);

        // [FIX] The original NPCData uses 'portraitSprite' not 'portrait'
        if (dialogueData.lines[0].speakerData != null && dialogueData.lines[0].speakerData.portraitSprite != null)
        {
            npcPortrait.style.backgroundImage = new StyleBackground(dialogueData.lines[0].speakerData.portraitSprite);
        }

        isDialogueActive = true;
        dialogueContainer.style.display = DisplayStyle.Flex;
        OnDialogueStateChanged?.Invoke(true);

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
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

        if (typeWriterCoroutine != null) StopCoroutine(typeWriterCoroutine);
        typeWriterCoroutine = StartCoroutine(TypewriterEffect(fullLine));
    }

    private void UpdateSpeakerUI()
    {
        // This logic will need to be updated when we re-implement the full branching dialogue system.
        // For now, it alternates speakers.
        bool isPlayerSpeaking = (currentDialogueData.lines.Count - currentDialogueLines.Count) % 2 == 0;

        if (isPlayerSpeaking)
        {
            speakerName.text = playerData.characterData.characterName;
            playerPortrait.AddToClassList("active");
            npcPortrait.RemoveFromClassList("active");
        }
        else
        {
            speakerName.text = currentDialogueData.lines[0].speakerData.npcName;
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
        OnDialogueStateChanged?.Invoke(false);
    }

    private void Update()
    {
        if (isDialogueActive && Input.anyKeyDown)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                return;

            DisplayNextLine();
        }
    }
}
