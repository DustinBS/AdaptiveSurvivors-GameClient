// GameClient/Assets/Scripts/UI/HealthBarUI.cs

using UnityEngine;
using UnityEngine.UI; // Required for UI components like Slider and Text
using TMPro; // Required for TextMeshPro UI elements

/// <summary>
/// Manages the player's health bar UI. It listens to the PlayerStatus component
/// for health changes and updates the UI elements accordingly.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI Slider component that visually represents the health bar.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Optional: A TextMeshProUGUI component to display health numerically (e.g., '100 / 100').")]
    [SerializeField] private TextMeshProUGUI healthText;

    private PlayerStatus playerStatus;

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
            Debug.LogError("HealthBarUI: PlayerStatus component not found in the scene. The health bar will not function.", this);
            gameObject.SetActive(false); // Disable the health bar if no player status is found.
        }
    }

    void OnDestroy()
    {
        // IMPORTANT: Always unsubscribe from events when the object is destroyed to prevent memory leaks.
        if (playerStatus != null)
        {
            playerStatus.OnHealthChanged -= UpdateHealthUI;
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
