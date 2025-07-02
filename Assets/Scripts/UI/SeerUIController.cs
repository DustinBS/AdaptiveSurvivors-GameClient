// GameClient/Assets/Scripts/UI/SeerUIController.cs

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using static KafkaClient;

/// <summary>
/// Manages the UI panel for the Ethereal Seer encounter. It populates the UI with
/// dialogue and bargain choices, and fires an event when a choice is made.
/// </summary>
public class SeerUIController : MonoBehaviour
{
    // Event to notify the SeerController that a choice has been made.
    public event Action<BargainChoice> OnBargainSelected;

    // --- UI Element References (using UI Toolkit as an example) ---
    private VisualElement rootElement;
    private Label dialogueLabel;
    private VisualElement choicesContainer;

    // --- Private State ---
    private List<BargainChoice> currentChoices;

    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            rootElement = uiDocument.rootVisualElement;
            // Query for the specific element names defined in SeerUI.uxml
            dialogueLabel = rootElement.Q<Label>("SeerDialogueLabel");
            choicesContainer = rootElement.Q<VisualElement>("BargainChoicesContainer");
        }
        else
        {
            Debug.LogError("SeerUIController expects a UIDocument component on the same GameObject.", this);
            this.enabled = false;
        }
    }

    /// <summary>
    /// Populates the UI with the Seer's data and displays it.
    /// </summary>
    /// <param name="dialogue">The dialogue line from the Seer.</param>
    /// <param name="choices">The list of bargain choices to display as buttons.</param>
    public void DisplayEncounter(string dialogue, List<BargainChoice> choices)
    {
        if (dialogueLabel == null || choicesContainer == null) return;

        dialogueLabel.text = dialogue;
        currentChoices = choices;

        choicesContainer.Clear();
        for (int i = 0; i < choices.Count; i++)
        {
            Button choiceButton = new Button
            {
                text = choices[i].description
            };
            choiceButton.AddToClassList("bargain-button"); // Apply our USS style

            int choiceIndex = i;
            choiceButton.clicked += () => OnChoiceButtonClicked(choiceIndex);

            choicesContainer.Add(choiceButton);
        }
    }

    /// <summary>
    /// Called when one of the dynamically created bargain buttons is clicked.
    /// </summary>
    private void OnChoiceButtonClicked(int choiceIndex)
    {
        if (currentChoices != null && choiceIndex >= 0 && choiceIndex < currentChoices.Count)
        {
            OnBargainSelected?.Invoke(currentChoices[choiceIndex]);
        }
    }
}