// GameClient/Assets/Scripts/Managers/LevelUpUIManager.cs
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

// Required for using the Addressables system
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Manages the Level-Up UI using an object pooling pattern. It now asynchronously loads
/// a default fallback icon from the Addressables system.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LevelUpUIManager : MonoBehaviour
{
    // --- Constants ---
    private const int MAX_CHOICES = 6;
    // The key/address of your fallback icon in the Addressables system.
    private const string DEFAULT_ICON_ADDRESS = "Icon_Upgrade";

    // --- UI Element References ---
    private VisualElement rootPanel;
    private VisualElement choicesContainer;

    // --- Object Pool ---
    private readonly List<VisualElement> _choiceCardPool = new List<VisualElement>();

    // --- Dependencies & Fallbacks ---
    private PlayerExperience playerExperience;
    // This field will cache the sprite once it's loaded from Addressables.
    private Sprite _defaultIconSprite;


    void Awake()
    {
        // --- Get Dependencies ---
        playerExperience = FindAnyObjectByType<PlayerExperience>();
        if (playerExperience == null)
        {
            Debug.LogError("LevelUpUIManager could not find a PlayerExperience component.", this);
            enabled = false;
            return;
        }

        // --- Query UI ---
        var uiDocument = GetComponent<UIDocument>();
        rootPanel = uiDocument.rootVisualElement.Q<VisualElement>("LevelUpPanel");
        choicesContainer = rootPanel.Q<VisualElement>("ChoicesContainer");

        // --- Start Asynchronous Loading ---
        LoadDefaultIcon();

        // --- Create the Object Pool ---
        CreateChoiceCardPool();

        // --- Final Setup ---
        HidePanel();
    }

    /// <summary>
    /// Asynchronously loads the default icon sprite from the Addressables system
    /// and caches it in the _defaultIconSprite field.
    /// </summary>
    private async void LoadDefaultIcon()
    {
        // Request the asset from Addressables using its key.
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(DEFAULT_ICON_ADDRESS);

        // Wait until the loading operation is complete.
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // If loading was successful, store the result.
            _defaultIconSprite = handle.Result;
            Debug.Log($"Default fallback icon '{DEFAULT_ICON_ADDRESS}' loaded successfully from Addressables.");
        }
        else
        {
            // If loading failed, log an error.
            Debug.LogError($"Failed to load default icon from Addressables. Address: '{DEFAULT_ICON_ADDRESS}'");
        }
    }


    /// <summary>
    /// Populates the pooled cards with new data and shows the required number.
    /// This method is now robust against null icons.
    /// </summary>
    private void PopulateUpgradeChoices(List<UpgradeData> choices)
    {
        for (int i = 0; i < _choiceCardPool.Count; i++)
        {
            var card = _choiceCardPool[i] as Button;
            // Clear any old callbacks to ensure a clean state.
            card.UnregisterCallback<ClickEvent, UpgradeChoiceEventData>(OnUpgradeChosen);

            if (i < choices.Count)
            {
                var upgradeData = choices[i];
                var icon = card.Q<VisualElement>("Icon");
                var titleLabel = card.Q<Label>("UpgradeTitle");
                var descriptionLabel = card.Q<Label>("UpgradeDescription");

                titleLabel.text = upgradeData.title;
                descriptionLabel.text = upgradeData.description;

                // --- ROBUST ICON LOGIC ---
                // 1. Prioritize the icon from the UpgradeData.
                // 2. If it's null, use our cached, asynchronously loaded default icon.
                Sprite iconToShow = upgradeData.icon != null ? upgradeData.icon : _defaultIconSprite;

                // 3. Only assign the background image if we actually have a sprite to show.
                if (iconToShow != null)
                {
                    icon.style.backgroundImage = new StyleBackground(iconToShow);
                }
                else
                {
                    // If both are null (e.g., default icon hasn't loaded yet), clear the image.
                    icon.style.backgroundImage = null;
                }

                var eventData = new UpgradeChoiceEventData { chosen = upgradeData, allOffered = choices };
                card.RegisterCallback<ClickEvent, UpgradeChoiceEventData>(OnUpgradeChosen, eventData);
                card.style.display = DisplayStyle.Flex;
            }
            else
            {
                card.style.display = DisplayStyle.None;
            }
        }
    }

    private void CreateChoiceCardPool()
    {
        for (int i = 0; i < MAX_CHOICES; i++)
        {
            var cardButton = new Button();
            cardButton.AddToClassList("upgrade-choice-card");
            var cardHeader = new VisualElement { name = "CardHeader" };
            cardHeader.AddToClassList("card-header");
            var icon = new VisualElement { name = "Icon" };
            icon.AddToClassList("upgrade-icon");
            var titleLabel = new Label { name = "UpgradeTitle" };
            titleLabel.AddToClassList("upgrade-title-label");
            var descriptionLabel = new Label { name = "UpgradeDescription" };
            descriptionLabel.AddToClassList("upgrade-description-label");
            cardHeader.Add(icon);
            cardHeader.Add(titleLabel);
            cardButton.Add(cardHeader);
            cardButton.Add(descriptionLabel);
            cardButton.style.display = DisplayStyle.None;
            choicesContainer.Add(cardButton);
            _choiceCardPool.Add(cardButton);
        }
    }
    private void OnEnable() { if (playerExperience != null) playerExperience.OnLevelUp += HandleLevelUp; }
    private void OnDisable() { if (playerExperience != null) playerExperience.OnLevelUp -= HandleLevelUp; }

    private void HandleLevelUp(int newLevel)
    {
        Time.timeScale = 0f;
        if (PlayerInputManager.Instance != null) PlayerInputManager.Instance.SwitchToUIControls();

        List<UpgradeData> upgradeChoices = playerExperience.GetUpgradeChoices();
        PopulateUpgradeChoices(upgradeChoices);
        ShowPanel();
    }

    private struct UpgradeChoiceEventData { public UpgradeData chosen; public List<UpgradeData> allOffered; }

    private void OnUpgradeChosen(ClickEvent evt, UpgradeChoiceEventData eventData)
    {
        playerExperience.ApplyUpgradeAndSendEvent(eventData.chosen, eventData.allOffered);
        Time.timeScale = 1f;
        if (PlayerInputManager.Instance != null) PlayerInputManager.Instance.SwitchToPlayerControls();
        HidePanel();
    }

    private void ShowPanel() => rootPanel.style.display = DisplayStyle.Flex;
    private void HidePanel() => rootPanel.style.display = DisplayStyle.None;
}
