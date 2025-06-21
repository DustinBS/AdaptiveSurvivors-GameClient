// GameClient/Assets/Scripts/Managers/LevelUpUIManager.cs
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class LevelUpUIManager : MonoBehaviour
{
    private VisualElement rootPanel;
    private VisualElement choicesContainer;
    private PlayerExperience playerExperience;

    void Awake()
    {
        playerExperience = FindAnyObjectByType<PlayerExperience>();
        if (playerExperience == null)
        {
            Debug.LogError("LevelUpUIManager could not find a PlayerExperience component.", this);
            enabled = false;
            return;
        }

        var uiDocument = GetComponent<UIDocument>();
        rootPanel = uiDocument.rootVisualElement.Q<VisualElement>("LevelUpPanel");
        choicesContainer = rootPanel.Q<VisualElement>("ChoicesContainer");

        HidePanel();
    }

    private void OnEnable()
    {
        if (playerExperience != null)
        {
            playerExperience.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (playerExperience != null)
        {
            playerExperience.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        Time.timeScale = 0f;

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SwitchToUIControls();
        }

        List<UpgradeData> upgradeChoices = playerExperience.GetUpgradeChoices();

        PopulateUpgradeChoices(upgradeChoices);
        ShowPanel();
    }

    /// <summary>
    /// Dynamically creates and populates the upgrade choice cards based on the provided data.
    /// </summary>
    private void PopulateUpgradeChoices(List<UpgradeData> choices)
    {
        // Clear any cards from the previous level-up
        choicesContainer.Clear();

        foreach (var upgradeData in choices)
        {
            // 1. Create the card Button element
            var cardButton = new Button();
            cardButton.AddToClassList("upgrade-choice-card");
            cardButton.userData = choices; // Store all choices for the event later
            cardButton.RegisterCallback<ClickEvent, UpgradeData>(OnUpgradeChosen, upgradeData);

            // 2. Create the card's internal structure
            // Header
            var cardHeader = new VisualElement();
            cardHeader.AddToClassList("card-header");

            // Icon
            var icon = new VisualElement();
            icon.AddToClassList("upgrade-icon");
            icon.style.backgroundImage = new StyleBackground(upgradeData.icon);

            // Title
            var titleLabel = new Label(upgradeData.title);
            titleLabel.AddToClassList("upgrade-title-label");

            // Description
            var descriptionLabel = new Label(upgradeData.description);
            descriptionLabel.AddToClassList("upgrade-description-label");

            // 3. Assemble the card
            cardHeader.Add(icon);
            cardHeader.Add(titleLabel);

            cardButton.Add(cardHeader);
            cardButton.Add(descriptionLabel);

            // 4. Add the finished card to the main container
            choicesContainer.Add(cardButton);
        }
    }

    private void OnUpgradeChosen(ClickEvent evt, UpgradeData chosenUpgrade)
    {
        var button = evt.currentTarget as Button;
        var allOfferedUpgrades = button.userData as List<UpgradeData>;

        playerExperience.ApplyUpgradeAndSendEvent(chosenUpgrade, allOfferedUpgrades);

        // No need to unregister callbacks since we Clear() the container each time

        Time.timeScale = 1f;
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SwitchToPlayerControls();
        }
        HidePanel();
    }

    private void ShowPanel() => rootPanel.style.display = DisplayStyle.Flex;
    private void HidePanel() => rootPanel.style.display = DisplayStyle.None;
}