// GameClient/Assets/Scripts/Enemy/EnemyHealth.cs

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages the health and identity of an enemy.
/// Now broadcasts events for both damage taken and death.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    // --- Public Fields ---
    [Header("Runtime Enemy Stats")]
    public string EnemyId;
    public string EnemyType;
    public float currentHealth;
    public float maxHealth;
    public float xpValue;

    // --- Static Events ---
    /// <summary>
    /// Event fired when an enemy takes damage.
    /// Parameters: Damage Amount (float), World Position (Vector3) for visual effects.
    /// </summary>
    public static event Action<float, Vector3> OnDamaged;

    /// <summary>
    /// Event fired when an enemy dies.
    /// Parameters: EnemyId (string), EnemyType (string), XP Value (float)
    /// </summary>
    public static event Action<string, string, float> OnEnemyDeath;

    // --- Private Fields ---
    private KafkaClient kafkaClient;
    private string playerId = "player_001";

    void Awake()
    {
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("EnemyHealth: KafkaClient not found in the scene.", this);
            enabled = false;
        }
        EnemyId = $"enemy_{GetInstanceID()}";
    }

    public void Initialize(EnemyData data)
    {
        EnemyType = data.enemyID;
        maxHealth = data.maxHealth;
        currentHealth = data.maxHealth;
        xpValue = data.xpValue;
    }

    public void TakeDamage(float amount, string sourceWeaponId)
    {
        if (!enabled || currentHealth <= 0) return;

        currentHealth -= amount;

        // --- NEW: Invoke the damage event for UI feedback ---
        OnDamaged?.Invoke(amount, transform.position);

        SendDamageTakenEvent(amount, sourceWeaponId);

        if (currentHealth <= 0)
        {
            Die(sourceWeaponId);
        }
    }

    private void Die(string killingWeaponId)
    {
        OnEnemyDeath?.Invoke(EnemyId, EnemyType, xpValue);
        // Prevent further actions on this already-dead enemy
        enabled = false;
        Destroy(gameObject);
    }

    private void SendDamageTakenEvent(float dmgAmount, string weaponId)
    {
        var payload = new Dictionary<string, object>
        {
            { "dmg_amount", dmgAmount },
            { "enemy_id", EnemyId },
            { "source_type", weaponId }
        };
        kafkaClient.SendGameplayEvent("damage_taken_event", playerId, payload);
    }

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
