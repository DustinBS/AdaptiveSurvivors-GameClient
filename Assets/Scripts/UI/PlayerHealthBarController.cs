// GameClient/Assets/Scripts/UI/PlayerHealthBarController.cs

using UnityEngine;
using UnityEngine.UIElements; // Required for UI Toolkit elements
using System.Collections;

/// <summary>
/// This script controls a health bar created with UI Toolkit.
/// It hooks into the PlayerStatus events to update the UXML visuals
/// in response to health changes.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PlayerHealthBarController : MonoBehaviour
{
    private VisualElement healthBarForeground;
    private Label healthLabel;

    private PlayerStatus playerStatus;

    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Query the UXML document to find the elements we named.
        // The '#' signifies a query by name, similar to a CSS ID selector.
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
        // Find the PlayerStatus component in the scene.
        playerStatus = FindObjectOfType<PlayerStatus>();
        if (playerStatus != null)
        {
            // Subscribe to the health changed event.
            playerStatus.OnHealthChanged += UpdateHealthUI;

            // Initialize the health bar with the player's starting health.
            UpdateHealthUI(playerStatus.currentHealth, playerStatus.maxHealth);
        }
        else
        {
            Debug.LogWarning("PlayerHealthBarController: PlayerStatus component not found in the scene. Hiding health bar.", this);
            // Hide the UI if there's no player to track.
            GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events when the object is destroyed to prevent memory leaks.
        if (playerStatus != null)
        {
            playerStatus.OnHealthChanged -= UpdateHealthUI;
        }
    }

    /// <summary>
    /// The callback function that updates the UI Toolkit visuals.
    /// </summary>
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        // Calculate the health percentage (a value from 0 to 100).
        float healthPercent = 0f;
        if (maxHealth > 0)
        {
            healthPercent = (currentHealth / maxHealth) * 100f;
        }

        // Update the width of the foreground element using a percentage.
        // This is how we change the visual length of the bar.
        healthBarForeground.style.width = new Length(healthPercent, LengthUnit.Percent);

        // Update the numerical text label.
        healthLabel.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }
}
