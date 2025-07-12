// GameClient/Assets/Scripts/UI/HealthBarUI.cs

using UnityEngine;
using UnityEngine.UI; // Required for UI components like Slider
using TMPro; // Required for TextMeshPro UI elements

/// <summary>
/// Manages the player's health bar UI. It listens to the central PlayerStats component
/// for health changes and updates the UI elements accordingly.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI Slider component that visually represents the health bar.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Optional: A TextMeshProUGUI component to display health numerically (e.g., '100 / 100').")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Dependencies")]
    [Tooltip("A reference to the AttributeRegistry asset. Used to find the MaxHealth attribute.")]
    [SerializeField] private AttributeRegistry attributeRegistry;
    
    private PlayerStats playerStats;

    void Start()
    {
        // Find the PlayerStats component in the scene.
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            // Subscribe to the health changed event from the new central stats component.
            playerStats.OnHealthChanged += UpdateHealthUI;

            // Initialize the health bar with the player's starting health.
            float maxHealth = playerStats.GetAttributeValue(attributeRegistry.MaxHealth);
            UpdateHealthUI(playerStats.currentHealth, maxHealth);
        }
        else
        {
            Debug.LogError("HealthBarUI: PlayerStats component not found in the scene. The health bar will not function.", this);
            gameObject.SetActive(false); // Disable the health bar if no player stats is found.
        }
    }

    void OnDestroy()
    {
        // IMPORTANT: Always unsubscribe from events when the object is destroyed to prevent memory leaks.
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthUI;
        }
    }

    /// <summary>
    /// Callback function that is triggered when the player's health changes.
    /// Updates the slider value and the optional text.
    /// </summary>
    /// <param name="currentHealth">The player's new current health.</param>
    /// <param name="maxHealth">The player's new maximum health.</param>
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        // Calculate the health percentage (a value between 0.0 and 1.0)
        float healthPercent = 0f;
        if (maxHealth > 0)
        {
            healthPercent = currentHealth / maxHealth;
        }

        // Update the slider's value. The slider's min/max should be set to 0 and 1.
        if (healthSlider != null)
        {
            healthSlider.value = healthPercent;
        }

        // Update the text display if it's assigned.
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}