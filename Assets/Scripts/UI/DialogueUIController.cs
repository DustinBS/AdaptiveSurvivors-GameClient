// GameClient/Assets/Scripts/UI/DialogueUIController.cs

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// This class acts as the dedicated View Controller for the Dialogue UI.
/// Its sole responsibility is to manage and manipulate the VisualElements defined
/// in the DialogueUI.uxml file. It decouples the UI from the game logic
/// by exposing public methods that the DialogueManager can call.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DialogueUIController : MonoBehaviour
{
    // --- Public Delegates & Enums ---
    public Action<PlayerResponse> OnChoiceSelected;
    public enum PortraitSide { Player, NPC }

    // --- UI Element References ---
    private VisualElement dialogueRoot;
    private VisualElement playerPortrait;
    private VisualElement npcPortrait;
    private Label speakerLabel;
    private Label dialogueText;
    private VisualElement continuePrompt;
    private VisualElement choiceButtonContainer;

    // --- USS Class Names ---
    private const string INACTIVE_SPEAKER_CLASS = "inactive-speaker";
    private const string SPEAKER_IS_PLAYER_CLASS = "speaker-is-player";
    private const string SPEAKER_IS_NPC_CLASS = "speaker-is-npc";
    private const string CHOICE_BUTTON_CLASS = "dialogue-choice-button";

    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Query all the necessary elements from the UXML file by their names
        dialogueRoot = root.Q<VisualElement>("DialogueRoot");
        playerPortrait = root.Q<VisualElement>("PlayerPortrait");
        npcPortrait = root.Q<VisualElement>("NPCPortrait");
        speakerLabel = root.Q<Label>("SpeakerLabel");
        dialogueText = root.Q<Label>("DialogueText");
        continuePrompt = root.Q<VisualElement>("ContinuePrompt");
        choiceButtonContainer = root.Q<VisualElement>("ChoiceButtonContainer");

        // Start with the UI hidden
        ShowDialogue(false);
    }

    /// <summary>
    /// Shows or hides the entire dialogue UI.
    /// </summary>
    public void ShowDialogue(bool show)
    {
        dialogueRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Updates the portrait images for both characters.
    /// </summary>
    public void UpdatePortraits(Sprite player, Sprite npc)
    {
        playerPortrait.style.backgroundImage = new StyleBackground(player);
        npcPortrait.style.backgroundImage = new StyleBackground(npc);
    }

    /// <summary>
    /// Sets the currently active speaker, updating their portrait tint and nameplate position.
    /// </summary>
    public void SetActiveSpeaker(PortraitSide side)
    {
        // First, remove positioning classes from the label to reset it
        speakerLabel.RemoveFromClassList(SPEAKER_IS_PLAYER_CLASS);
        speakerLabel.RemoveFromClassList(SPEAKER_IS_NPC_CLASS);

        if (side == PortraitSide.Player)
        {
            // Highlight player, dim NPC
            playerPortrait.RemoveFromClassList(INACTIVE_SPEAKER_CLASS);
            npcPortrait.AddToClassList(INACTIVE_SPEAKER_CLASS);
            speakerLabel.AddToClassList(SPEAKER_IS_PLAYER_CLASS);
        }
        else
        {
            // Highlight NPC, dim player
            playerPortrait.AddToClassList(INACTIVE_SPEAKER_CLASS);
            npcPortrait.RemoveFromClassList(INACTIVE_SPEAKER_CLASS);
            speakerLabel.AddToClassList(SPEAKER_IS_NPC_CLASS);
        }
    }

    /// <summary>
    /// Sets the name and text for the current dialogue line.
    /// </summary>
    public void SetDialogueLine(string speakerName, string text)
    {
        speakerLabel.text = speakerName;
        dialogueText.text = text;
    }

    /// <summary>
    /// Shows or hides the continue prompt icon.
    /// </summary>
    public void ShowContinuePrompt(bool show)
    {
        continuePrompt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Displays a set of player choices as buttons.
    /// </summary>
    public void DisplayChoices(List<PlayerResponse> choices)
    {
        choiceButtonContainer.Clear(); // Remove any old choices
        choiceButtonContainer.style.display = DisplayStyle.Flex;

        foreach (var choice in choices)
        {
            var button = new Button(() => OnChoiceSelected?.Invoke(choice))
            {
                text = choice.responseText
            };
            button.AddToClassList(CHOICE_BUTTON_CLASS);
            choiceButtonContainer.Add(button);
        }
    }

    /// <summary>
    /// Hides and clears the player choice buttons.
    /// </summary>
    public void HideChoices()
    {
        choiceButtonContainer.style.display = DisplayStyle.None;
        choiceButtonContainer.Clear();
    }
}
