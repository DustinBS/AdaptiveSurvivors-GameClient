// GameClient/Assets/Scripts/Enemy/EnemyHealth.cs

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages the health and identity of an enemy. Its stats are initialized from an EnemyData ScriptableObject.
/// It sends 'damage_taken_event' and 'enemy_death_event' to Kafka and fires a C# event on death.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Runtime Enemy Stats")]
    [Tooltip("Unique identifier for this enemy instance.")]
    public string EnemyId;

    [Tooltip("The type of this enemy (e.g., 'basic_grunt', 'adaptive_elite').")]
    public string EnemyType;

    [Tooltip("The current health of the enemy.")]
    public float currentHealth;

    [Tooltip("The maximum health of the enemy, set at initialization.")]
    public float maxHealth;

    [Tooltip("The XP value granted on death, set at initialization.")]
    public float xpValue;

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    // The player ID is needed for associating events in the backend.
    private string playerId = "player_001"; // This can be set by the spawner or a GameManager

    /// <summary>
    /// Event fired when an enemy dies.
    /// Parameters: EnemyId (string), EnemyType (string), XP Value (float)
    /// </summary>
    public static event Action<string, string, float> OnEnemyDeath;

    void Awake()
    {
        kafkaClient = FindObjectOfType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("EnemyHealth: KafkaClient not found in the scene.", this);
            enabled = false;
        }

        // A unique runtime ID is assigned to distinguish this specific instance from others of the same type.
        EnemyId = $"enemy_{GetInstanceID()}";
    }

    /// <summary>
    /// Initializes the enemy's stats from an EnemyData ScriptableObject.
    /// This method should be called by the EnemySpawner immediately after instantiation.
    /// </summary>
    /// <param name="data">The EnemyData asset defining this enemy.</param>
    public void Initialize(EnemyData data)
    {
        EnemyType = data.enemyID; // Use the scriptable object's ID as the type
        maxHealth = data.maxHealth;
        currentHealth = data.maxHealth;
        xpValue = data.xpValue;
    }

    /// <summary>
    /// Reduces the enemy's health and sends a damage_taken_event to Kafka.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    /// <param name="sourceWeaponId">The ID of the weapon that dealt the damage.</param>
    public void TakeDamage(float amount, string sourceWeaponId)
    {
        if (!enabled) return;

        currentHealth -= amount;

        SendDamageTakenEvent(amount, sourceWeaponId);

        if (currentHealth <= 0)
        {
            Die(sourceWeaponId);
        }
    }

    /// <summary>
    /// Handles enemy death: sends enemy_death_event, invokes the C# event, and destroys the GameObject.
    /// </summary>
    /// <param name="killingWeaponId">The ID of the weapon that delivered the final blow.</param>
    private void Die(string killingWeaponId)
    {
        SendEnemyDeathEvent(killingWeaponId);

        // Invoke the static event for other systems (like PlayerExperience) to react to the death.
        // We pass all relevant information.
        OnEnemyDeath?.Invoke(EnemyId, EnemyType, xpValue);

        // In a real game, you might pool this object instead of destroying it.
        Destroy(gameObject);
    }

    /// <summary>
    /// Sends a damage_taken_event to Kafka.
    /// </summary>
    private void SendDamageTakenEvent(float dmgAmount, string weaponId)
    {
        var payload = new Dictionary<string, object>
        {
            { "dmg_amount", dmgAmount },
            { "enemy_id", EnemyId },
            { "source_type", weaponId } // The GDD implies sourceType can be the weapon ID
        };
        kafkaClient.SendGameplayEvent("damage_taken_event", playerId, payload);
    }

    /// <summary>
    /// Sends an enemy_death_event to Kafka.
    /// </summary>
    private void SendEnemyDeathEvent(string killingWeaponId)
    {
        var payload = new Dictionary<string, object>
        {
            { "enemy_id", EnemyId },
            { "enemy_type", EnemyType },
            { "killed_by_weapon_id", killingWeaponId }
        };
        kafkaClient.SendGameplayEvent("enemy_death_event", playerId, payload);
    }
}
