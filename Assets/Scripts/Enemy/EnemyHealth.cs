// GameClient/Assets/Scripts/Enemy/EnemyHealth.cs

using UnityEngine;
using System.Collections.Generic; // For Dictionary
using System; // For Action delegate

// This script manages the health of an enemy.
// It also sends damage_taken_event and enemy_death_event to Kafka.
// Attach this script to any enemy GameObject.

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Settings")]
    [Tooltip("Unique identifier for this enemy instance.")]
    public string EnemyId = "enemy_001"; // Assign a unique ID per enemy instance

    [Tooltip("The type of this enemy (e.g., 'basic_grunt', 'adaptive_elite').")]
    public string EnemyType = "basic_grunt";

    [Tooltip("The current health of the enemy.")]
    public float currentHealth;

    [Tooltip("The maximum health of the enemy.")]
    public float maxHealth = 100f;

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    // Player ID to associate with Kafka events (e.g., who dealt the damage/killed)
    [Tooltip("ID of the player interacting with this enemy (usually the main player).")]
    public string playerId = "player_001"; // Should match the main player's ID

    // Event fired when an enemy dies, allowing other scripts to subscribe (e.g., PlayerExperience to gain XP)
    public static event Action<string, string> OnEnemyDeath; // EnemyId, EnemyType

    void Awake()
    {
        currentHealth = maxHealth;

        // Updated to use FindAnyObjectByType to resolve deprecation warning
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("EnemyHealth: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
        }

        // Assign a unique ID if not set in inspector
        if (string.IsNullOrEmpty(EnemyId) || EnemyId == "enemy_001")
        {
            EnemyId = "enemy_" + GetInstanceID(); // Simple unique ID based on instance
        }
    }

    /// <summary>
    /// Reduces the enemy's health and sends a damage_taken_event to Kafka.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    /// <param name="sourceType">The source of the damage (e.g., 'player_attack', 'environment').</param>
    public void TakeDamage(float amount, string sourceType)
    {
        if (!enabled) return; // Don't take damage if script is disabled

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} ({EnemyId}) took {amount} damage. Current Health: {currentHealth}");

        // Send damage_taken_event to Kafka
        SendDamageTakenEvent(amount, sourceType);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles enemy death: sends enemy_death_event to Kafka and destroys the GameObject.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} ({EnemyId}) died!");

        // Send enemy_death_event to Kafka
        SendEnemyDeathEvent();

        // Invoke the OnEnemyDeath event so other scripts (like PlayerExperience) can react
        OnEnemyDeath?.Invoke(EnemyId, EnemyType);

        // In a real game, you might play death animations, drop loot, etc.
        Destroy(gameObject);
    }

    /// <summary>
    /// Sends a damage_taken_event to Kafka.
    /// </summary>
    /// <param name="dmgAmount">The amount of damage taken.</param>
    /// <param name="sourceType">The type of source that dealt the damage.</param>
    private void SendDamageTakenEvent(float dmgAmount, string sourceType)
    {
        var payload = new Dictionary<string, object>
        {
            { "dmg_amount", dmgAmount },
            { "enemy_id", EnemyId },
            { "source_type", sourceType }
        };
        kafkaClient.SendGameplayEvent("damage_taken_event", playerId, payload);
    }

    /// <summary>
    /// Sends an enemy_death_event to Kafka.
    /// </summary>
    private void SendEnemyDeathEvent()
    {
        // For simplicity, we'll assume the player's last hit killed the enemy.
        // In a complex system, you might track the last attacker.
        // Updated to use FindAnyObjectByType to resolve deprecation warning
        string killedByWeaponId = FindAnyObjectByType<PlayerAttack>()?.weaponId ?? "unknown"; // Get player's weapon ID if available

        var payload = new Dictionary<string, object>
        {
            { "enemy_id", EnemyId },
            { "enemy_type", EnemyType },
            { "killed_by_weapon_id", killedByWeaponId }
        };
        kafkaClient.SendGameplayEvent("enemy_death_event", playerId, payload);
    }
}
