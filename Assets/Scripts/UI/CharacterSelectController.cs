// GameClient/Assets/Scripts/UI/CharacterSelectController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class CharacterSelectController : MonoBehaviour
{
    [Header("Data References")]
    [Tooltip("The main PlayerData asset to be configured for the run.")]
    [SerializeField] private PlayerData mainPlayerData;
    [Tooltip("A list of all selectable character archetypes.")]
    [SerializeField] private List<CharacterData> availableCharacters;

    [Header("Scene Configuration")]
    [Tooltip("The name of the scene to load after character selection.")]
    [SerializeField] private string nextSceneName = "Hub";

    // UI Element References
    private VisualElement jarGrid;
    private Label characterNameLabel;
    private Label characterDescriptionLabel;
    private Label statHealthLabel;
    private Label statDamageLabel;
    private Label statSpeedLabel;
    private VisualElement statHealthRegenContainer;
    private Label statHealthRegenLabel;
    private Label statHealthRegenPercentLabel;
    private Label statUpgradeChoicesLabel;
    private Button playButton;

    // State Management
    private CharacterData selectedCharacter;
    private VisualElement selectedJarSlot;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Query all UI elements
        jarGrid = root.Q<VisualElement>("JarGrid");
        characterNameLabel = root.Q<Label>("CharacterName");
        characterDescriptionLabel = root.Q<Label>("CharacterDescription");
        statHealthLabel = root.Q<Label>("StatHealth");
        statDamageLabel = root.Q<Label>("StatDamage");
        statSpeedLabel = root.Q<Label>("StatSpeed");
        statHealthRegenContainer = root.Q<VisualElement>("StatHealthRegenContainer");
        statHealthRegenLabel = root.Q<Label>("StatHealthRegen");
        statHealthRegenPercentLabel = root.Q<Label>("StatHealthRegenPercent");
        statUpgradeChoicesLabel = root.Q<Label>("StatUpgradeChoices");
        playButton = root.Q<Button>("PlayButton");

        // Set initial state
        playButton.SetEnabled(false);
        PopulateJarGrid();
        ResetInfoPanel();

        // Register button callback
        playButton.clicked += OnPlayButtonClicked;
    }

    /// <summary>
    /// Dynamically creates the jar slots and populates them with available characters.
    /// </summary>
    private void PopulateJarGrid()
    {
        jarGrid.Clear();
        for (int i = 0; i < 9; i++) // 3x3 grid
        {
            var jarSlot = new VisualElement();
            jarSlot.AddToClassList("jar-slot");

            if (i < availableCharacters.Count)
            {
                CharacterData character = availableCharacters[i];
                jarSlot.AddToClassList("filled");

                var headPortrait = new VisualElement();
                headPortrait.AddToClassList("jar-head-portrait");
                headPortrait.style.backgroundImage = new StyleBackground(character.characterSprite);
                jarSlot.Add(headPortrait);

                // Register a callback for when this jar is clicked
                jarSlot.RegisterCallback<ClickEvent>(evt => SelectCharacter(character, jarSlot));
            }

            jarGrid.Add(jarSlot);
        }
    }

    /// <summary>
    /// Called when a character jar is clicked. Updates state and UI.
    /// </summary>
    private void SelectCharacter(CharacterData character, VisualElement jarSlotElement)
    {
        // Deselect previous jar if one was selected
        if (selectedJarSlot != null)
        {
            selectedJarSlot.RemoveFromClassList("selected");
        }

        selectedCharacter = character;
        selectedJarSlot = jarSlotElement;

        // Add selection highlight to the new jar
        selectedJarSlot.AddToClassList("selected");

        UpdateInfoPanel(character);
        playButton.SetEnabled(true);
    }

    /// <summary>
    /// Updates the left info panel with the stats of the provided character.
    /// </summary>
    private void UpdateInfoPanel(CharacterData character)
    {
        characterNameLabel.text = character.characterName.ToUpper();
        characterDescriptionLabel.text = character.description;
        statHealthLabel.text = $"Base Health: {character.baseHealth}";
        statDamageLabel.text = $"Base Damage: {character.baseDamage}";
        statSpeedLabel.text = $"Speed Multiplier: {character.speedMultiplier}x";

        // Handle unique stats visibility and text
        if (character.hasHealthRegen)
        {
            statHealthRegenContainer.style.display = DisplayStyle.Flex;
            statHealthRegenLabel.text = "Health Regen: Yes";
            statHealthRegenPercentLabel.text = $"({character.healthRegenPercent * 100}%)";
        }
        else
        {
            statHealthRegenContainer.style.display = DisplayStyle.Flex; // Keep container visible for consistent layout
            statHealthRegenLabel.text = "Health Regen: No";
            statHealthRegenPercentLabel.text = "";
        }

        int totalUpgradeChoices = character.defaultUpgradeChoices + (character.hasExtraUpgradeChoice ? character.extraUpgradeChoices : 0);
        statUpgradeChoicesLabel.text = $"Upgrade Choices: {totalUpgradeChoices}";
    }

    /// <summary>
    /// Resets the info panel to its default placeholder state.
    /// </summary>
    private void ResetInfoPanel()
    {
        characterNameLabel.text = "SELECT A HEAD";
        characterDescriptionLabel.text = "Choose your destiny from the fluid-filled jars.";
        statHealthLabel.text = "Base Health: --";
        statDamageLabel.text = "Base Damage: --";
        statSpeedLabel.text = "Speed Multiplier: --";
        statHealthRegenContainer.style.display = DisplayStyle.Flex;
        statHealthRegenLabel.text = "Health Regen: --";
        statHealthRegenPercentLabel.text = "";
        statUpgradeChoicesLabel.text = "Upgrade Choices: --";
    }

    /// <summary>
    /// Called when the "ADAPT" button is clicked. Configures player data and loads the next scene.
    /// </summary>
    private void OnPlayButtonClicked()
    {
        if (selectedCharacter == null)
        {
            // This case should ideally not happen since the button is disabled,
            // but it's good practice to have a fallback.
            Debug.LogWarning("No character selected!");
            return;
        }

        // --- This is the critical step ---
        // 1. Assign the chosen character archetype to the main PlayerData asset.
        mainPlayerData.characterData = selectedCharacter;
        // 2. Initialize the PlayerData stats for a new run.
        mainPlayerData.InitializeForRun();

        Debug.Log($"Character '{selectedCharacter.characterName}' selected. Loading scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }
}
