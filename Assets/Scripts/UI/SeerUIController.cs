// GameClient/Assets/Scripts/UI/SeerUIController.cs

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using System;

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
    private Coroutine loadingAnimationCoroutine;

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
    /// Displays the initial UI state with a fallback "no bargain" option.
    /// </summary>
    public void DisplayInitialState(string initialDialogue)
    {
        if (dialogueLabel == null || choicesContainer == null) return;

        dialogueLabel.text = initialDialogue;
        choicesContainer.Clear();

        // Add a disabled, animated "loading" button
        Button loadingButton = new Button { text = "Awaiting vision..." };
        loadingButton.SetEnabled(false);
        loadingButton.AddToClassList("bargain-button");
        choicesContainer.Add(loadingButton);

        loadingAnimationCoroutine = StartCoroutine(AnimateLoadingText(loadingButton));

        // Add the permanent "No Bargain" option
        AddNoBargainButton();
    }

    /// <summary>
    /// Updates the UI with the real choices received from the backend.
    /// </summary>
    public void UpdateWithBargains(string dialogue, List<BargainChoice> choices)
    {
        if (dialogueLabel == null || choicesContainer == null) return;

        // Stop the loading animation if it's running
        if (loadingAnimationCoroutine != null)
        {
            StopCoroutine(loadingAnimationCoroutine);
        }

        dialogueLabel.text = dialogue;
        currentChoices = choices;

        choicesContainer.Clear(); // Clear loading buttons and old "no bargain" button

        // Add the real bargain buttons
        for (int i = 0; i < choices.Count; i++)
        {
            Debug.Log($"[SeerUI] Creating button for choice: {choices[i].description}");

            Button choiceButton = new Button { text = choices[i].description };
            choiceButton.AddToClassList("bargain-button");

            int choiceIndex = i;
            choiceButton.clicked += () => OnChoiceButtonClicked(choiceIndex);
            choicesContainer.Add(choiceButton);
        }

        // Re-add the "No Bargain" button at the end
        AddNoBargainButton();
    }

    private void AddNoBargainButton()
    {
        Button noBargainButton = new Button { text = "I will face my fate alone." };
        noBargainButton.AddToClassList("bargain-button");
        // Add a unique style class if you want to color it differently
        noBargainButton.AddToClassList("no-bargain-button");
        noBargainButton.clicked += () => OnChoiceButtonClicked(-1); // Use -1 to signify "no bargain"
        choicesContainer.Add(noBargainButton);
    }

    /// <summary>
    /// Called when one of the dynamically created bargain buttons is clicked.
    /// </summary>
    private void OnChoiceButtonClicked(int choiceIndex)
    {
        // -1 case for the "no bargain" option.
        if (choiceIndex == -1)
        {
            OnBargainSelected?.Invoke(null);
            return; // Exit the method
        }

        if (currentChoices != null && choiceIndex >= 0 && choiceIndex < currentChoices.Count)
        {
            OnBargainSelected?.Invoke(currentChoices[choiceIndex]);
        }
    }

    private IEnumerator AnimateLoadingText(Button loadingButton)
    {
        int dotCount = 1;
        while (true)
        {
            dotCount = (dotCount % 3) + 1;
            string dots = new string('.', dotCount);
            loadingButton.text = "Awaiting vision" + dots;
            yield return new WaitForSeconds(0.5f);
        }
    }
}