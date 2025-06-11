// GameClient/Assets/Scripts/Player/PlayerExperience.cs

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the player's experience points (XP) and level-up progression.
/// It listens for enemy death events, handles level-ups, and presents upgrade choices
/// based on a pool of UpgradeData ScriptableObjects.
/// </summary>
public class PlayerExperience : MonoBehaviour
{
    [Header("Experience Settings")]
    [Tooltip("The unique ID of the player.")]
    public string playerId = "player_001";

    [Tooltip("The current level of the player.")]
    public int currentLevel = 1;

    [Tooltip("The current experience points of the player.")]
    public float currentXP = 0f;

    [Tooltip("The amount of XP required for the next level.")]
    public float xpToNextLevel = 100f;

    [Header("Upgrade System")]
    [Tooltip("The list of all possible UpgradeData assets that can be offered to the player.")]
    public List<UpgradeData> upgradePool;

    [Tooltip("Number of upgrade options presented to the player at each level-up.")]
    [Range(1, 5)]
    public int numberOfUpgradeChoices = 3;

    // References to other components on the Player GameObject
    private KafkaClient kafkaClient;
    private PlayerStatus playerStatus;
    private PlayerAttack playerAttack;
    private PlayerMovement playerMovement;

    // Events for UI updates
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnXPChanged;

    void Awake()
    {
        // Cache references to all necessary components
        // Using FindAnyObjectByType as it's faster if any instance is acceptable.
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        playerStatus = GetComponent<PlayerStatus>();
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>();

        if (kafkaClient == null || playerStatus == null || playerAttack == null || playerMovement == null)
        {
            Debug.LogError("PlayerExperience: Missing one or more required components (KafkaClient, PlayerStatus, PlayerAttack, PlayerMovement).", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        // Correctly subscribe to the event.
        EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    }

    void OnDisable()
    {
        // Correctly unsubscribe from the event.
        EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;
    }

    void Start()
    {
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    /// <summary>
    /// Adds XP to the player and checks for level-up condition.
    /// </summary>
    public void AddXP(float amount)
    {
        currentXP += amount;
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);

        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        xpToNextLevel = 100f + (currentLevel - 1) * 50f; // Simple scaling formula

        Debug.Log($"LEVEL UP! Player is now Level {currentLevel}.");

        OnLevelUp?.Invoke(currentLevel);
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);

        PresentUpgradeChoices();
    }

    /// <summary>
    /// Selects random, unique upgrades from the pool and applies the player's choice.
    /// </summary>
    private void PresentUpgradeChoices()
    {
        if (upgradePool == null || upgradePool.Count == 0)
        {
            Debug.LogWarning("Upgrade pool is empty. No upgrades to offer.");
            return;
        }

        var random = new System.Random();
        var offeredUpgrades = upgradePool.OrderBy(x => random.Next()).Take(numberOfUpgradeChoices).ToList();

        if (offeredUpgrades.Count > 0)
        {
            // In a real implementation, you would trigger a UI panel here.
            Debug.Log("Choose your upgrade:");
            foreach (var upgrade in offeredUpgrades)
            {
                Debug.Log($"- {upgrade.title}: {upgrade.description}");
            }

            // --- Player Choice Simulation ---
            UpgradeData chosenUpgrade = offeredUpgrades[0];
            Debug.Log($"Player chose: {chosenUpgrade.title}");

            ApplyUpgrade(chosenUpgrade);
            SendUpgradeChoiceEvent(chosenUpgrade, offeredUpgrades);
        }
    }

    /// <summary>
    /// Applies the effects of the chosen upgrade to the relevant player components.
    /// </summary>
    private void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.upgradeType)
        {
            case UpgradeType.MaxHealth:
                playerStatus.maxHealth += upgrade.value;
                playerStatus.Heal(upgrade.value); // Also heal for the increased amount
                break;
            case UpgradeType.MoveSpeed:
                playerMovement.moveSpeed *= (1 + upgrade.value);
                break;
            case UpgradeType.WeaponDamage:
                // Note: Modifying ScriptableObject at runtime is possible but affects all instances.
                // A better approach for stats is to have local variables in PlayerAttack that are initialized
                // from the SO and then modified. For simplicity now, we modify the SO instance directly.
                playerAttack.currentWeapon.baseDamage *= (1 + upgrade.value);
                playerAttack.InitializeWeaponStats(); // Re-initialize to apply changes
                break;
            case UpgradeType.AttackSpeed:
                playerAttack.currentWeapon.attackInterval *= (1 - upgrade.value);
                playerAttack.InitializeWeaponStats(); // Re-initialize to apply changes
                break;
            default:
                Debug.LogWarning($"Upgrade type '{upgrade.upgradeType}' not implemented yet.");
                break;
        }
    }

    /// <summary>
    /// Sends an `upgrade_choice_event` to Kafka.
    /// </summary>
    private void SendUpgradeChoiceEvent(UpgradeData chosenUpgrade, List<UpgradeData> offeredUpgrades)
    {
        var rejectedIds = offeredUpgrades.Where(u => u != chosenUpgrade).Select(u => u.upgradeID).ToList();
        var payload = new Dictionary<string, object>
        {
            { "lvl", currentLevel },
            { "chosen_upgrade_id", chosenUpgrade.upgradeID },
            { "rejected_ids", rejectedIds }
        };
        kafkaClient.SendGameplayEvent("upgrade_choice_event", playerId, payload);
    }

    /// <summary>
    /// Event handler that is called when an enemy is defeated.
    /// [FIX] The method signature now correctly matches the 'OnEnemyDeath' event delegate.
    /// </summary>
    private void HandleEnemyDeath(string enemyId, string enemyType, float xpValue)
    {
        AddXP(xpValue);
    }
}