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
    private string playerId;
    private int numberOfUpgradeChoices;

    [Header("Experience Settings")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 10f;

    [Header("Upgrade System")]
    [Tooltip("The list of all possible UpgradeData assets that can be offered to the player.")]
    public List<UpgradeData> masterUpgradePool;
    // --- Private State ---
    // This list tracks the unique, non-repeatable upgrades the player has already acquired this run.
    private List<UpgradeData> _acquiredUniqueUpgrades = new List<UpgradeData>();

    private KafkaClient kafkaClient;
    private PlayerStatus playerStatus;
    private PlayerAttack playerAttack;
    private PlayerMovement playerMovement;

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnXPChanged;

    // The Initialize method, called by PlayerInitializer
    public void Initialize(CharacterData data)
    {
        this.playerId = data.characterName;
        this.numberOfUpgradeChoices = data.defaultUpgradeChoices + data.extraUpgradeChoices;
    }

    void Awake()
    {
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        playerStatus = GetComponent<PlayerStatus>();
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>();

        if (kafkaClient == null || playerStatus == null || playerAttack == null || playerMovement == null)
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
    /// Selects a number of random, unique upgrades from the pool. This logic is now
    /// robust and correctly handles repeatable upgrades as fallbacks.
    /// </summary>
    public List<UpgradeData> GetUpgradeChoices()
    {
        if (masterUpgradePool == null || masterUpgradePool.Count == 0)
        {
            Debug.LogWarning("Master Upgrade Pool is empty. No upgrades to offer.");
            return new List<UpgradeData>();
        }
        // 1. Create a pool of all valid candidates for this level-up.
        // An upgrade is a valid candidate if it's repeatable, OR if it's a unique upgrade the player has not yet acquired.
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

        // 2. Get a list of *distinct* candidates to prioritize unique offerings on a single panel.
        var distinctCandidates = candidatePool.Distinct().ToList();
        int choicesToMake = this.numberOfUpgradeChoices;

        for (int i = 0; i < choicesToMake; i++)
        {
            if (distinctCandidates.Count > 0)
            {
                // Prioritize picking from the distinct list first.
                int index = random.Next(distinctCandidates.Count);
                var choice = distinctCandidates[index];
                offeredUpgrades.Add(choice);
                distinctCandidates.RemoveAt(index); // Remove to avoid offering the same unique item twice on one panel.
            }
            else
            {
                // If we've run out of distinct options (e.g., we need 3 choices but only have 2 unique candidates left),
                // we fall back to picking any item from the full candidate pool, which allows for repeatables to fill the slots.
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
        // If the chosen upgrade is unique, add it to our tracking list so we don't offer it again.
        if (!chosenUpgrade.isRepeatable && !_acquiredUniqueUpgrades.Contains(chosenUpgrade))
        {
            _acquiredUniqueUpgrades.Add(chosenUpgrade);
        }

        ApplyUpgrade(chosenUpgrade);
        SendUpgradeChoiceEvent(chosenUpgrade, offeredUpgrades);
    }

    /// <summary>
    /// Applies the effects of the chosen upgrade to the relevant player components.
    /// This logic is now clean and calls authoritative methods on other components.
    /// </summary>
    private void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.upgradeType)
        {
            case UpgradeType.MaxHealth:
                playerStatus.ModifyMaxHealth(upgrade.value, upgrade.isPercentage);
                break;

            case UpgradeType.WeaponDamage:
                playerAttack.ModifyDamage(upgrade.value, upgrade.isPercentage);
                break;

            case UpgradeType.MoveSpeed:
                playerMovement.ModifyMoveSpeed(upgrade.value, upgrade.isPercentage);
                break;

            case UpgradeType.AttackSpeed:
                playerAttack.ModifyAttackSpeed(upgrade.value, upgrade.isPercentage);
                break;

            // Add other cases like AttackSpeed, etc. following the same pattern.
            // case UpgradeType.AttackSpeed:
            //     playerAttack.ModifyAttackSpeed(upgrade.value, upgrade.isPercentage);
            //     break;

            default:
                Debug.LogWarning($"Upgrade type '{upgrade.upgradeType}' not implemented in PlayerExperience.cs");
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
        kafkaClient.SendGameplayEvent("upgrade_choice", playerId, payload);
    }

    /// <summary>
    /// Event handler that is called when an enemy is defeated.
    /// </summary>
    private void HandleEnemyDefeated(EnemyData defeatedEnemyData)
    {
        AddXP(defeatedEnemyData.xpValue);
    }
}