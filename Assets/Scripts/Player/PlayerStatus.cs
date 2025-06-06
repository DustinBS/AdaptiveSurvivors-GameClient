// GameClient/Assets/Scripts/Player/PlayerStatus.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .ToList() and other LINQ operations

// This script manages the player's health, mana, and active buffs/debuffs.
// It sends player_status_event to Kafka periodically and handles incoming damage.

public class PlayerStatus : MonoBehaviour
{
    [Header("Player Stats")]
    [Tooltip("The unique ID of the player. Should match other player scripts.")]
    public string playerId = "player_001";

    [Tooltip("The maximum health points of the player.")]
    public float maxHealth = 100f;
    [Tooltip("The current health points of the player.")]
    public float currentHealth;

    [Tooltip("The maximum mana points of the player. Set to 0 if mana is not used.")]
    public float maxMana = 50f;
    [Tooltip("The current mana points of the player.")]
    public float currentMana;

    [Tooltip("List of currently active buffs on the player.")]
    public List<string> activeBuffs = new List<string>();
    [Tooltip("List of currently active debuffs on the player.")]
    public List<string> activeDebuffs = new List<string>();

    [Header("Kafka Event Settings")]
    [Tooltip("How often (in seconds) player_status_event is sent to Kafka.")]
    public float statusEventSendInterval = 1.0f; // Send status every 1 second

    private KafkaClient kafkaClient;
    private float statusEventTimer;

    void Awake()
    {
        currentHealth = maxHealth;
        currentMana = maxMana; // Initialize mana

        kafkaClient = FindAnyObjectByType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("PlayerStatus: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
        }

        statusEventTimer = statusEventSendInterval;
    }

    void Update()
    {
        // Periodically send player status to Kafka
        statusEventTimer -= Time.deltaTime;
        if (statusEventTimer <= 0)
        {
            SendPlayerStatusEvent();
            statusEventTimer = statusEventSendInterval;
        }
    }

    /// <summary>
    /// Reduces the player's health.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    /// <param name="sourceEnemyId">The ID of the enemy that dealt the damage (optional).</param>
    public void TakeDamage(float amount, string sourceEnemyId = "unknown_enemy")
    {
        if (!enabled) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"Player took {amount} damage from {sourceEnemyId}. Current HP: {currentHealth}/{maxHealth}");

        // Optionally, send a specific damage_taken_event for the player as well,
        // although the GDD only specified it for enemies.
        // If the Flink job needs to know how much damage *player* takes, this would be the place.
        // For now, focusing on the GDD's player_status_event.

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Restores player health.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        Debug.Log($"Player healed for {amount}. Current HP: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Consumes mana.
    /// </summary>
    /// <param name="amount">The amount of mana to consume.</param>
    /// <returns>True if mana was consumed successfully, false otherwise (e.g., not enough mana).</returns>
    public bool ConsumeMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            Debug.Log($"Player consumed {amount} mana. Current Mana: {currentMana}/{maxMana}");
            return true;
        }
        Debug.Log($"Not enough mana to consume {amount}. Current Mana: {currentMana}/{maxMana}");
        return false;
    }

    /// <summary>
    /// Adds mana to the player.
    /// </summary>
    /// <param name="amount">The amount of mana to add.</param>
    public void AddMana(float amount)
    {
        currentMana += amount;
        if (currentMana > maxMana) currentMana = maxMana;
        Debug.Log($"Player gained {amount} mana. Current Mana: {currentMana}/{maxMana}");
    }


    /// <summary>
    /// Adds a buff to the player.
    /// </summary>
    /// <param name="buffName">The name of the buff.</param>
    public void AddBuff(string buffName)
    {
        if (!activeBuffs.Contains(buffName))
        {
            activeBuffs.Add(buffName);
            Debug.Log($"Buff added: {buffName}");
            // Trigger visual/gameplay effects of the buff
        }
    }

    /// <summary>
    /// Removes a buff from the player.
    /// </summary>
    /// <param name="buffName">The name of the buff to remove.</param>
    public void RemoveBuff(string buffName)
    {
        if (activeBuffs.Remove(buffName))
        {
            Debug.Log($"Buff removed: {buffName}");
            // Remove visual/gameplay effects of the buff
        }
    }

    /// <summary>
    /// Adds a debuff to the player.
    /// </summary>
    /// <param name="debuffName">The name of the debuff.</param>
    public void AddDebuff(string debuffName)
    {
        if (!activeDebuffs.Contains(debuffName))
        {
            activeDebuffs.Add(debuffName);
            Debug.Log($"Debuff added: {debuffName}");
            // Trigger visual/gameplay effects of the debuff
        }
    }

    /// <summary>
    /// Removes a debuff from the player.
    /// </summary>
    /// <param name="debuffName">The name of the debuff to remove.</param>
    public void RemoveDebuff(string debuffName)
    {
        if (activeDebuffs.Remove(debuffName))
        {
            Debug.Log($"Debuff removed: {debuffName}");
            // Remove visual/gameplay effects of the debuff
        }
    }

    /// <summary>
    /// Handles player death.
    /// </summary>
    private void Die()
    {
        Debug.Log("Player has died!");
        // Trigger game over sequence, load meta-progression screen etc.
        // For POC, simply disable the player object.
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sends the current player status (HP, mana, buffs, debuffs) to Kafka.
    /// </summary>
    private void SendPlayerStatusEvent()
    {
        var payload = new Dictionary<string, object>
        {
            { "hp", currentHealth },
            { "mana", currentMana },
            { "active_buffs", activeBuffs.ToList() }, // Convert HashSet to List for JSON serialization
            { "active_debuffs", activeDebuffs.ToList() }
        };

        kafkaClient.SendGameplayEvent("player_status_event", playerId, payload);
        // Debug.Log("Player status event sent to Kafka.");
    }

    // Example of how other scripts might call this to deal damage:
    // var playerStatus = GameObject.FindAnyObjectByType<PlayerStatus>(); // Corrected for deprecation
    // if (playerStatus != null) playerStatus.TakeDamage(10f, "enemy_attack");
}
