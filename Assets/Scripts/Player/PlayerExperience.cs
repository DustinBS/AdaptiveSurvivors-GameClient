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
    [Header("Dependencies")]
    [Tooltip("A reference to the AttributeRegistry asset. Used to access specific attribute data.")]
    [SerializeField] private AttributeRegistry attributeRegistry;

    [Header("Experience Settings")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 10f;

    [Header("Upgrade System")]
    [Tooltip("The list of all possible UpgradeData assets that can be offered to the player.")]
    public List<UpgradeData> masterUpgradePool;

    // --- Private State ---
    private List<UpgradeData> _acquiredUniqueUpgrades = new List<UpgradeData>();

    // --- Component References ---
    private KafkaClient kafkaClient;
    private PlayerStats playerStats;

    // --- Events ---
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnXPChanged;
    
    // The Initialize method is no longer needed, as this component gets its data from PlayerStats.
    // public void Initialize(CharacterData data, string newPlayerId) { ... }

    void Awake()
    {
        // Get references to other components.
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        playerStats = GetComponent<PlayerStats>();

        if (kafkaClient == null || playerStats == null)
        {
            Debug.LogError("PlayerExperience: Missing one or more required components.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        // Subscribe to the new, consolidated event.
        EnemyHealth.OnEnemyDefeated += HandleEnemyDefeated;
    }

    void OnDisable()
    {
        // Unsubscribe from the new, consolidated event.
        EnemyHealth.OnEnemyDefeated -= HandleEnemyDefeated;
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
        xpToNextLevel = xpToNextLevel * 1.1f; // Simple scaling formula

        Debug.Log($"LEVEL UP! Player is now Level {currentLevel}.");

        StatisticsTracker.RecordFinalLevel(currentLevel);

        OnLevelUp?.Invoke(currentLevel);
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    /// <summary>
    /// Selects a number of random, unique upgrades from the pool.
    /// </summary>
    public List<UpgradeData> GetUpgradeChoices()
    {
        if (masterUpgradePool == null || masterUpgradePool.Count == 0)
        {
            Debug.LogWarning("Master Upgrade Pool is empty. No upgrades to offer.");
            return new List<UpgradeData>();
        }
        
        // Fetch the number of choices from our new Attribute System.
        int defaultChoices = (int)playerStats.GetAttributeValue(attributeRegistry.DefaultUpgradeChoices);
        int extraChoices = (int)playerStats.GetAttributeValue(attributeRegistry.ExtraUpgradeChoices);
        int choicesToMake = defaultChoices + extraChoices;

        var candidatePool = masterUpgradePool
            .Where(upgrade => upgrade.isRepeatable || !_acquiredUniqueUpgrades.Contains(upgrade))
            .ToList();

        var offeredUpgrades = new List<UpgradeData>();
        var random = new System.Random();

        if (candidatePool.Count == 0)
        {
            Debug.LogError("FATAL: No upgrades available in the candidate pool! Check your upgrade data asset settings.");
            return offeredUpgrades; // Return empty list
        }
        
        var distinctCandidates = candidatePool.Distinct().ToList();

        for (int i = 0; i < choicesToMake; i++)
        {
            if (distinctCandidates.Count > 0)
            {
                int index = random.Next(distinctCandidates.Count);
                var choice = distinctCandidates[index];
                offeredUpgrades.Add(choice);
                distinctCandidates.RemoveAt(index);
            }
            else
            {
                Debug.LogWarning("[DEBUG] Ran out of distinct candidates. Falling back to the full candidate pool to fill remaining slots.");
                int index = random.Next(candidatePool.Count);
                offeredUpgrades.Add(candidatePool[index]);
            }
        }
        return offeredUpgrades;
    }

    /// <summary>
    /// Applies the chosen upgrade and tracks it if it's not repeatable.
    /// </summary>
    public void ApplyUpgradeAndSendEvent(UpgradeData chosenUpgrade, List<UpgradeData> offeredUpgrades)
    {
        if (!chosenUpgrade.isRepeatable && !_acquiredUniqueUpgrades.Contains(chosenUpgrade))
        {
            _acquiredUniqueUpgrades.Add(chosenUpgrade);
        }

        ApplyUpgrade(chosenUpgrade);
        SendUpgradeChoiceEvent(chosenUpgrade, offeredUpgrades);
    }

    /// <summary>
    /// Applies the effects of the chosen upgrade to the relevant player components.
    /// </summary>
    private void ApplyUpgrade(UpgradeData upgrade)
    {
        // --- 1. Apply Permanent Stat Modification ---
        if (upgrade.attributeToModify != null)
        {
            // Call the new method in PlayerStats for permanent upgrades.
            playerStats.ApplyPermanentModifier(upgrade.attributeToModify, upgrade.value, upgrade.isPercentage);
        }

        // --- 2. Apply Behavioral Modification ---
        if (upgrade.behaviorComponentPrefab != null)
        {
            Instantiate(upgrade.behaviorComponentPrefab, transform);
            Debug.Log($"Added new behavior: {upgrade.behaviorComponentPrefab.name}");
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
        // Get playerID from the central PlayerStats component
        kafkaClient.SendGameplayEvent("upgrade_choice", playerStats.playerID, payload);
    }

    /// <summary>
    /// Event handler that is called when an enemy is defeated.
    /// </summary>
    private void HandleEnemyDefeated(EnemyData defeatedEnemyData)
    {
        AddXP(defeatedEnemyData.xpValue);
    }
}