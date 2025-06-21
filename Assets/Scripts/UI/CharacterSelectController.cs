// GameClient/Assets/Scripts/UI/CharacterSelectController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the logic for the Character Selection screen.
/// It dynamically populates the UI with available characters, handles player
/// selection, updates the displayed information, and prepares the PlayerData
/// for the main game run.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class CharacterSelectController : MonoBehaviour
{
    [Header("Data References")]
    [Tooltip("A list of all character archetypes available for selection.")]
    public List<CharacterData> availableCharacters;

    [Tooltip("The PlayerData ScriptableObject that will store the selected character and persist across scenes.")]
    public PlayerData playerData;

    [Header("Scene Configuration")]
    [Tooltip("The name of the scene to load after a character is selected.")]
    public string sceneToLoad = "Hub";

    // UI Element References
    private VisualElement _root;
    private VisualElement _jarGrid;
    private Button _adaptButton;
    private Label _characterNameLabel;
    private Label _characterDescriptionLabel;
    private Label _baseHealthLabel;
    private Label _baseDamageLabel;
    private Label _speedMultiplierLabel;
    private VisualElement _healthRegenContainer;
    private Label _healthRegenLabel;
    private Label _upgradeChoicesLabel;

    // State
    private CharacterData _selectedCharacter;
    private VisualElement _selectedJarElement;
    private const string SELECTED_JAR_CLASS = "character-jar--selected";

    private void OnEnable()
    {
        // Get the root VisualElement from the UIDocument component
        _root = GetComponent<UIDocument>().rootVisualElement;

        // --- Query for all necessary UI elements by name ---
        _jarGrid = _root.Q<VisualElement>("JarGrid");
        _adaptButton = _root.Q<Button>("AdaptButton");
        _characterNameLabel = _root.Q<Label>("CharacterName");
        _characterDescriptionLabel = _root.Q<Label>("CharacterDescription");
        _baseHealthLabel = _root.Q<Label>("BaseHealthLabel");
        _baseDamageLabel = _root.Q<Label>("BaseDamageLabel");
        _speedMultiplierLabel = _root.Q<Label>("SpeedMultiplierLabel");
        _healthRegenContainer = _root.Q<VisualElement>("HealthRegenContainer");
        _healthRegenLabel = _root.Q<Label>("HealthRegenLabel");
        _upgradeChoicesLabel = _root.Q<Label>("UpgradeChoicesLabel");

        // --- Register Callbacks ---
        _adaptButton.clicked += OnAdaptButtonClicked;

        // Defer the initial UI population until the UI has been fully laid out and styled.
        // This prevents style race conditions.
        _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnDisable()
    {
        // It's good practice to unregister callbacks when the object is disabled
        if (_adaptButton != null)
        {
            _adaptButton.clicked -= OnAdaptButtonClicked;
        }
        // Also unregister the geometry change event in case the object is disabled before it fires.
        if (_root != null)
        {
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    /// <summary>
    /// This callback is fired once the UI has been constructed, laid out, and styled for the first time.
    /// It's the safest point to perform initial UI population and manipulation.
    /// </summary>
    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        // --- Initial State Setup ---
        PopulateCharacterGrid();
        ResetInfoPanel();

        // The setup is complete, so we unregister the callback to prevent it from running again.
        _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    /// <summary>
    /// Populates the JarGrid with UI elements for each available character.
    /// </summary>
    private void PopulateCharacterGrid()
    {
        if (_jarGrid == null)
        {
            Debug.LogError("JarGrid VisualElement not found in the UXML.");
            return;
        }

        _jarGrid.Clear(); // Clear any existing elements

        foreach (var character in availableCharacters)
        {
            // Create the container for the jar
            var jarElement = new VisualElement();
            jarElement.AddToClassList("character-jar");

            // Create the portrait image
            var portraitImage = new VisualElement();
            portraitImage.AddToClassList("character-portrait-image");
            portraitImage.style.backgroundImage = new StyleBackground(character.characterPortrait);

            jarElement.Add(portraitImage);

            // Register a callback for when this jar is clicked.
            // We pass the character and the jar's VisualElement to the selection handler.
            jarElement.RegisterCallback<ClickEvent>(evt => HandleCharacterSelected(character, jarElement));

            _jarGrid.Add(jarElement);
        }
    }

    /// <summary>
    /// Handles the logic for when a character jar is clicked.
    /// </summary>
    /// <param name="character">The CharacterData associated with the clicked jar.</param>
    /// <param name="jarElement">The VisualElement of the jar that was clicked.</param>
    private void HandleCharacterSelected(CharacterData character, VisualElement jarElement)
    {
        // If there was a previously selected jar, remove its "selected" style
        if (_selectedJarElement != null)
        {
            _selectedJarElement.RemoveFromClassList(SELECTED_JAR_CLASS);
        }

        // Mark the new character and element as selected
        _selectedCharacter = character;
        _selectedJarElement = jarElement;
        _selectedJarElement.AddToClassList(SELECTED_JAR_CLASS);

        // Update the info panel with the new character's details
        UpdateInfoPanel();

        // Enable the "ADAPT" button
        _adaptButton.SetEnabled(true);
    }

    /// <summary>
    /// Updates the left-side info panel with the details of the currently selected character.
    /// </summary>
    private void UpdateInfoPanel()
    {
        if (_selectedCharacter == null)
        {
            ResetInfoPanel();
            return;
        }

        _characterNameLabel.text = _selectedCharacter.characterName.ToUpper();
        _characterDescriptionLabel.text = _selectedCharacter.description;
        _baseHealthLabel.text = $"Base Health: {_selectedCharacter.baseHealth}";
        _baseDamageLabel.text = $"Base Damage: {_selectedCharacter.baseDamage}";
        _speedMultiplierLabel.text = $"Speed Multiplier: {_selectedCharacter.speedMultiplier}x";

        // Handle the health regen display logic
        if (_selectedCharacter.hasHealthRegen)
        {
            _healthRegenContainer.style.display = DisplayStyle.Flex;
            _healthRegenLabel.text = $"Health Regen: Yes ({_selectedCharacter.healthRegenPercent}%)";
        }
        else
        {
            _healthRegenContainer.style.display = DisplayStyle.None;
        }

        int totalUpgradeChoices = _selectedCharacter.defaultUpgradeChoices + _selectedCharacter.extraUpgradeChoices;
        _upgradeChoicesLabel.text = $"Upgrade Choices: {totalUpgradeChoices}";
    }

    /// <summary>
    /// Resets the info panel to its default, unselected state.
    /// </summary>
    private void ResetInfoPanel()
    {
        _characterNameLabel.text = "SELECT A HEAD";
        _characterDescriptionLabel.text = "Choose your destiny... a new form awaits. Select a specimen from the lab to begin the adaptation process.";
        _baseHealthLabel.text = "Base Health: --";
        _baseDamageLabel.text = "Base Damage: --";
        _speedMultiplierLabel.text = "Speed Multiplier: --";
        _healthRegenContainer.style.display = DisplayStyle.Flex; // Show the container but with default text
        _healthRegenLabel.text = "Health Regen: --";
        _upgradeChoicesLabel.text = "Upgrade Choices: --";
    }

    /// <summary>
    /// Called when the "ADAPT" button is clicked. It finalizes the character
    /// choice and loads the main game scene.
    /// </summary>
    private void OnAdaptButtonClicked()
    {
        if (_selectedCharacter == null)
        {
            Debug.LogWarning("Adapt button was clicked, but no character is selected.");
            return;
        }

        if (playerData == null)
        {
            Debug.LogError("Cannot start game: PlayerData reference is not set in the inspector!");
            return;
        }

        // 1. Assign the chosen character to the PlayerData object
        playerData.characterData = _selectedCharacter;

        // 2. Initialize the PlayerData for the new run
        playerData.InitializeForRun();

        // 3. Load the "Hub" scene
        Debug.Log($"Loading scene '{sceneToLoad}'...");
        SceneManager.LoadScene(sceneToLoad);
    }
}
