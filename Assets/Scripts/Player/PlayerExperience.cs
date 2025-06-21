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
    public float xpToNextLevel = 2f;

    [Header("Upgrade System")]
    [Tooltip("The list of all possible UpgradeData assets that can be offered to the player.")]
    public List<UpgradeData> upgradePool;

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
    }

    /// <summary>
    /// Selects a number of random, unique upgrades from the pool to be presented to the player.
    /// called by the Level up UI Manager.
    /// </summary>
    /// <returns>A list of UpgradeData options.</returns>
    public List<UpgradeData> GetUpgradeChoices()
    {
        if (upgradePool == null || upgradePool.Count == 0)
        {
            Debug.LogWarning("Upgrade pool is empty. No upgrades to offer.");
            return new List<UpgradeData>();
        }

        var random = new System.Random();
        var offeredUpgrades = upgradePool.OrderBy(x => random.Next()).Take(numberOfUpgradeChoices).ToList();
        return offeredUpgrades;
    }

    /// <summary>
    /// Applies the effects of the chosen upgrade and sends the relevant telemetry event.
    /// called by the level up UI Manager after the player clicks a button.
    /// </summary>
    public void ApplyUpgradeAndSendEvent(UpgradeData chosenUpgrade, List<UpgradeData> offeredUpgrades)
    {
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
                // CORRECT: Tell PlayerStatus to handle its own max health increase.
                playerStatus.IncreaseMaxHealth(upgrade.value);
                break;
            
            case UpgradeType.WeaponDamage:
                // CORRECT: Tell PlayerAttack to handle its own damage increase.
                playerAttack.IncreaseDamage(upgrade.value, upgrade.isPercentage);
                break;

            case UpgradeType.MoveSpeed:
                // CORRECT: Tell PlayerMovement to handle its own speed increase.
                playerMovement.IncreaseMoveSpeed(upgrade.value, upgrade.isPercentage);
                break;

            // Add other cases like AttackSpeed, etc. following the same pattern.
            // case UpgradeType.AttackSpeed:
            //     playerAttack.IncreaseAttackSpeed(upgrade.value, upgrade.isPercentage);
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
    /// [FIX] The method signature now correctly matches the 'OnEnemyDeath' event delegate.
    /// </summary>
    private void HandleEnemyDeath(string enemyId, string enemyType, float xpValue)
    {
        AddXP(xpValue);
    }
}