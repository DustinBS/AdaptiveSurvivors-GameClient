// GameClient/Assets/Scripts/Player/PlayerExperience.cs

using UnityEngine;
using System;
using System.Collections.Generic;

// This script manages the player's experience points (XP) and level-up progression.
// It listens for enemy death events to gain XP, calculates level-ups,
// and manages a simple upgrade choice mechanism.
// It also sends 'upgrade_choice_event' to Kafka.

public class PlayerExperience : MonoBehaviour
{
    [Header("Experience Settings")]
    [Tooltip("The unique ID of the player.")]
    public string playerId = "player_001"; // Should match other player scripts

    [Tooltip("The current experience points of the player.")]
    public float currentXP = 0f;

    [Tooltip("The current level of the player.")]
    public int currentLevel = 1;

    [Tooltip("The amount of XP required for the next level. This can be a formula later.")]
    public float xpToNextLevel = 100f; // Initial XP for Level 2

    [Header("Upgrade Settings")]
    [Tooltip("List of possible upgrade options (e.g., 'MoveSpeedBoost', 'DamageIncrease', 'HealthRegen').")]
    public List<string> availableUpgrades = new List<string>
    {
        "MoveSpeedBoost", "DamageIncrease", "MaxHealthIncrease", "AttackSpeedIncrease", "MagnetRangeBoost", "CooldownReduction"
    };

    [Tooltip("Number of upgrade options presented to the player at each level-up.")]
    public int numberOfUpgradeChoices = 3;

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    // UI elements would typically be handled here or by a separate UI manager
    // For now, we'll just log messages.
    public Action<int> OnLevelUp; // Event for other scripts to subscribe to (e.g., UI)
    public Action<float, float> OnXPChanged; // Event for UI to update XP bar

    void Awake()
    {
        kafkaClient = FindObjectOfType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("PlayerExperience: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
        }

        // Initialize XP bar if UI exists
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    void OnEnable()
    {
        // Subscribe to EnemyHealth's OnDeath event to gain XP
        EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;
    }

    /// <summary>
    /// Adds XP to the player.
    /// </summary>
    /// <param name="amount">The amount of XP to add.</param>
    public void AddXP(float amount)
    {
        currentXP += amount;
        OnXPChanged?.Invoke(currentXP, xpToNextLevel); // Notify UI

        Debug.Log($"Gained {amount} XP. Current XP: {currentXP}/{xpToNextLevel}");

        // Check for level up
        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    /// <summary>
    /// Handles the level-up process.
    /// </summary>
    private void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToNextLevel; // Carry over excess XP
        xpToNextLevel = CalculateNextLevelXP(currentLevel); // Calculate XP for the next level

        Debug.Log($"LEVEL UP! Player is now Level {currentLevel}. XP to next: {xpToNextLevel}");

        OnLevelUp?.Invoke(currentLevel); // Notify other systems about level up
        OnXPChanged?.Invoke(currentXP, xpToNextLevel); // Update UI

        // Pause game (optional) and present upgrade choices
        PresentUpgradeChoices();
    }

    /// <summary>
    /// Calculates the XP required for the given level.
    /// This is a simple linear progression, can be changed to exponential or other formulas.
    /// </summary>
    /// <param name="level">The target level.</param>
    /// <returns>The XP required for that level.</returns>
    private float CalculateNextLevelXP(int level)
    {
        // Example: XP increases by 50 for each level
        return 100f + (level - 1) * 50f;
    }

    /// <summary>
    /// Selects random upgrade choices and presents them to the player.
    /// For this POC, it just logs the choices. In a real game, this would
    /// open an upgrade selection UI.
    /// </summary>
    private void PresentUpgradeChoices()
    {
        // Select random unique upgrades
        List<string> chosenUpgrades = new List<string>();
        HashSet<int> chosenIndices = new HashSet<int>();

        System.Random rnd = new System.Random(); // Using System.Random for simple selection

        while (chosenUpgrades.Count < numberOfUpgradeChoices && chosenUpgrades.Count < availableUpgrades.Count)
        {
            int randomIndex = rnd.Next(0, availableUpgrades.Count);
            if (!chosenIndices.Contains(randomIndex))
            {
                chosenIndices.Add(randomIndex);
                chosenUpgrades.Add(availableUpgrades[randomIndex]);
            }
        }

        Debug.Log("Choose your upgrade:");
        for (int i = 0; i < chosenUpgrades.Count; i++)
        {
            Debug.Log($"{i + 1}. {chosenUpgrades[i]}");
        }

        // For demonstration, we'll auto-select the first option and send the event.
        // In a real game, player input would determine `chosenUpgradeId`.
        if (chosenUpgrades.Count > 0)
        {
            string autoChosenUpgrade = chosenUpgrades[0];
            Debug.Log($"Auto-choosing: {autoChosenUpgrade}");
            ApplyUpgrade(autoChosenUpgrade);
            SendUpgradeChoiceEvent(currentLevel, autoChosenUpgrade, chosenUpgrades);
        }
        else
        {
            Debug.LogWarning("No upgrade options available!");
        }
    }

    /// <summary>
    /// Applies the chosen upgrade to the player's stats or abilities.
    /// This is a placeholder and should be expanded based on actual upgrade effects.
    /// </summary>
    /// <param name="upgradeId">The ID of the chosen upgrade.</param>
    private void ApplyUpgrade(string upgradeId)
    {
        // Example: Apply actual stat changes here
        Debug.Log($"Applying upgrade: {upgradeId}");
        // Example: if (upgradeId == "MoveSpeedBoost") playerMovement.IncreaseSpeed(0.1f);
        // This would require references to other player stat scripts.
    }

    /// <summary>
    /// Sends an `upgrade_choice_event` to Kafka.
    /// </summary>
    /// <param name="level">The player's current level.</param>
    /// <param name="chosenUpgradeId">The ID of the upgrade chosen by the player.</param>
    /// <param name="rejectedIds">A list of upgrade IDs that were presented but not chosen.</param>
    private void SendUpgradeChoiceEvent(int level, string chosenUpgradeId, List<string> allChoices)
    {
        List<string> rejectedIds = new List<string>(allChoices);
        rejectedIds.Remove(chosenUpgradeId); // Remove the chosen one to get rejected ones

        var payload = new Dictionary<string, object>
        {
            { "lvl", level },
            { "chosen_upgrade_id", chosenUpgradeId },
            { "rejected_ids", rejectedIds }
        };
        kafkaClient.SendGameplayEvent("upgrade_choice_event", playerId, payload);
    }

    /// <summary>
    /// Event handler for enemy death, used to add XP.
    /// </summary>
    /// <param name="enemyId">The ID of the killed enemy.</param>
    /// <param name="enemyType">The type of the killed enemy.</param>
    private void HandleEnemyDeath(string enemyId, string enemyType)
    {
        // Example: Grant XP based on enemy type
        float xpGranted = 10f; // Default XP
        if (enemyType.Contains("elite")) // Grant more XP for elites
        {
            xpGranted = 30f;
        }
        AddXP(xpGranted);
    }
}
