// GameClient/Assets/Scripts/UI/PlayerHealthBarController.cs

using UnityEngine;
using UnityEngine.UIElements; // Required for UI Toolkit elements
using System.Collections;

/// <summary>
/// This script controls a health bar created with UI Toolkit.
/// It hooks into the central PlayerStats component events to update the UXML visuals
/// in response to health changes.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PlayerHealthBarController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("A reference to the AttributeRegistry asset. Used to find the MaxHealth attribute.")]
    [SerializeField] private AttributeRegistry attributeRegistry;

    private VisualElement healthBarForeground;
    private Label healthLabel;

    private PlayerStats playerStats; // The instance of the component

    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        healthBarForeground = root.Q<VisualElement>("HealthBarForeground");
        healthLabel = root.Q<Label>("HealthLabel");

        if (healthBarForeground == null || healthLabel == null)
        {
            Debug.LogError("PlayerHealthBarController: Could not find 'HealthBarForeground' or 'HealthLabel' elements in the UXML document.", this);
            enabled = false;
        }
    }

    void Start()
    {
        // Find the PlayerStats component instance in the scene.
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            // Subscribe to the event on the INSTANCE.
            playerStats.OnHealthChanged += UpdateHealthUI;

            // Initialize the health bar with the player's starting health.
            // Get max health by calling the method on the INSTANCE.
            float maxHealth = playerStats.GetAttributeValue(attributeRegistry.MaxHealth);
            UpdateHealthUI(playerStats.currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning("PlayerHealthBarController: PlayerStats component not found in the scene. Hiding health bar.", this);
            GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from the event on the INSTANCE.
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        float healthPercent = (maxHealth > 0) ? (currentHealth / maxHealth) * 100f : 0f;

        healthBarForeground.style.width = new Length(healthPercent, LengthUnit.Percent);
        healthLabel.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }
}