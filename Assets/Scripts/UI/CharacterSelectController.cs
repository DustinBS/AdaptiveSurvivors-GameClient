// GameClient/Assets/Scripts/UI/CharacterSelectController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Linq; // Required for LINQ queries like FirstOrDefault

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

    [Tooltip("A reference to the AttributeRegistry asset. Used to find which stats to display.")]
    [SerializeField] private AttributeRegistry attributeRegistry;

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
        if (_adaptButton != null) _adaptButton.clicked -= OnAdaptButtonClicked;
        // Also unregister the geometry change event in case the object is disabled before it fires.
        if (_root != null) _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    /// <summary>
    /// This callback is fired once the UI has been constructed, laid out, and styled for the first time.
    /// It's the safest point to perform initial UI population and manipulation.
    /// </summary>
    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        PopulateCharacterGrid();
        ResetInfoPanel();
        _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    /// <summary>
    /// Populates the JarGrid with UI elements for each available character.
    /// </summary>
    private void PopulateCharacterGrid()
    {
        if (_jarGrid == null) return;
        _jarGrid.Clear();

        foreach (var character in availableCharacters)
        {
            var jarElement = new VisualElement();
            jarElement.AddToClassList("character-jar");
            var portraitImage = new VisualElement();
            portraitImage.AddToClassList("character-portrait-image");
            portraitImage.style.backgroundImage = new StyleBackground(character.characterPortrait);
            jarElement.Add(portraitImage);
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
        if (_selectedJarElement != null) _selectedJarElement.RemoveFromClassList(SELECTED_JAR_CLASS);

        _selectedCharacter = character;
        _selectedJarElement = jarElement;
        _selectedJarElement.AddToClassList(SELECTED_JAR_CLASS);

        UpdateInfoPanel();
        _adaptButton.SetEnabled(true);
    }

    /// <summary>
    /// Updates the left-side info panel by reading from the character's baseAttributes list.
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

        // Fetch values using our new data-driven helper method
        _baseHealthLabel.text = $"Base Health: {GetBaseAttributeValue(_selectedCharacter, attributeRegistry.MaxHealth)}";
        _baseDamageLabel.text = $"Damage Multiplier: {GetBaseAttributeValue(_selectedCharacter, attributeRegistry.CharacterDamageMultiplier)}";
        _speedMultiplierLabel.text = $"Speed Multiplier: {GetBaseAttributeValue(_selectedCharacter, attributeRegistry.MoveSpeed)}x";

        float regenValue = GetBaseAttributeValue(_selectedCharacter, attributeRegistry.HealthRegen);
        if (regenValue > 0)
        {
            _healthRegenContainer.style.display = DisplayStyle.Flex;
            _healthRegenLabel.text = $"Health Regen: Yes ({regenValue}%)";
        }
        else
        {
            _healthRegenContainer.style.display = DisplayStyle.None;
        }

        float defaultChoices = GetBaseAttributeValue(_selectedCharacter, attributeRegistry.DefaultUpgradeChoices);
        float extraChoices = GetBaseAttributeValue(_selectedCharacter, attributeRegistry.ExtraUpgradeChoices);
        _upgradeChoicesLabel.text = $"Upgrade Choices: {defaultChoices + extraChoices}";
    }

    /// <summary>
    /// A helper method to safely find a base attribute's value from a CharacterData.
    /// </summary>
    /// <returns>The value of the attribute, or 0 if not found.</returns>
    private float GetBaseAttributeValue(CharacterData data, AttributeData attribute)
    {
        if (data == null || attribute == null) return 0;

        var foundAttribute = data.baseAttributes.FirstOrDefault(attr => attr.attribute == attribute);
        return (foundAttribute != null) ? foundAttribute.value : 0;
    }

    private void ResetInfoPanel()
    {
        _characterNameLabel.text = "SELECT A HEAD";
        _characterDescriptionLabel.text = "Choose your destiny... a new form awaits. Select a specimen from the lab to begin the adaptation process.";
        _baseHealthLabel.text = "Base Health: --";
        _baseDamageLabel.text = "Damage Multiplier: --";
        _speedMultiplierLabel.text = "Speed Multiplier: --";
        _healthRegenContainer.style.display = DisplayStyle.Flex;
        _healthRegenLabel.text = "Health Regen: --";
        _upgradeChoicesLabel.text = "Upgrade Choices: --";
        _adaptButton.SetEnabled(false);
    }

    /// <summary>
    /// Called when the "ADAPT" button is clicked. It finalizes the character
    /// choice and loads the main game scene.
    /// </summary>
    private void OnAdaptButtonClicked()
    {
        if (_selectedCharacter == null || playerData == null) return;

        playerData.characterData = _selectedCharacter;
        playerData.InitializeForRun();
        SceneManager.LoadScene(sceneToLoad);
    }
}