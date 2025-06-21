// GameClient/Assets/Scripts/Managers/LevelUpUIManager.cs

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Manages the presentation of the level-up UI. It listens for the player's level-up
/// event, displays upgrade choices, and communicates the player's selection back to the core game systems.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LevelUpUIManager : MonoBehaviour
{
    private VisualElement rootPanel;
    private PlayerExperience playerExperience;
    private List<Button> choiceButtons = new List<Button>();

    private void Awake()
    {
        // Find the player experience component in the scene.
        playerExperience = FindAnyObjectByType<PlayerExperience>();
        if (playerExperience == null)
        {
            Debug.LogError("LevelUpUIManager could not find a PlayerExperience component in the scene.", this);
            enabled = false;
            return;
        }

        // Get UI elements from the UIDocument.
        var uiDocument = GetComponent<UIDocument>();
        rootPanel = uiDocument.rootVisualElement.Q<VisualElement>("LevelUpPanel");

        // Query for the buttons once and store them.
        choiceButtons.Add(rootPanel.Q<Button>("UpgradeChoiceCard1"));
        choiceButtons.Add(rootPanel.Q<Button>("UpgradeChoiceCard2"));
        choiceButtons.Add(rootPanel.Q<Button>("UpgradeChoiceCard3"));

        // The panel should be disabled by default.
        HidePanel();
    }

    private void OnEnable()
    {
        // Subscribe to the OnLevelUp event.
        if (playerExperience != null)
        {
            playerExperience.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks.
        if (playerExperience != null)
        {
            playerExperience.OnLevelUp -= HandleLevelUp;
        }
    }

    /// <summary>
    /// The event handler called when PlayerExperience.OnLevelUp is invoked.
    /// </summary>
    private void HandleLevelUp(int newLevel)
    {
        // Pause the game and get the available upgrade choices.
        Time.timeScale = 0f;

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SwitchToUIControls();
        }

        List<UpgradeData> upgradeChoices = playerExperience.GetUpgradeChoices();

        PopulateUpgradeChoices(upgradeChoices);
        ShowPanel();
    }


    private void PopulateUpgradeChoices(List<UpgradeData> choices)
    {
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            var button = choiceButtons[i];
            if (i < choices.Count)
            {
                var upgradeData = choices[i];

                // Populate the UI elements with data from the ScriptableObject.
                button.visible = true;
                button.Q<Label>("UpgradeTitle").text = upgradeData.title;
                button.Q<Label>("UpgradeDescription").text = upgradeData.description;
                button.Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(upgradeData.icon);

                // Register a one-time callback for the button click.
                button.RegisterCallback<ClickEvent, UpgradeData>(OnUpgradeChosen, upgradeData);
                // Store the full list of choices in user data to pass to the event.
                button.userData = choices;
            }
            else
            {
                // Hide any unused button slots.
                button.visible = false;
            }
        }
    }

    /// <summary>
    /// The callback executed when an upgrade button is clicked.
    /// </summary>
    private void OnUpgradeChosen(ClickEvent evt, UpgradeData chosenUpgrade)
    {
        var button = evt.currentTarget as Button;
        var allOfferedUpgrades = button.userData as List<UpgradeData>;

        // Apply the upgrade and send the telemetry event.
        playerExperience.ApplyUpgradeAndSendEvent(chosenUpgrade, allOfferedUpgrades);

        // Unregister all callbacks to prevent multiple triggers.
        foreach (var btn in choiceButtons)
        {
            // We pass the same method reference used for registering to unregister.
            btn.UnregisterCallback<ClickEvent, UpgradeData>(OnUpgradeChosen);
        }

        // Unpause the game and hide the panel.
        Time.timeScale = 1f;
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SwitchToPlayerControls();
        }
        HidePanel();
    }

    private void ShowPanel()
    {
        rootPanel.style.display = DisplayStyle.Flex; //
    }

    private void HidePanel()
    {
        rootPanel.style.display = DisplayStyle.None; //
    }
}