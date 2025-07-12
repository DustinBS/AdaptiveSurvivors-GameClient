// GameClient/Assets/Scripts/Enemy/EnemyHealth.cs

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages the health of an enemy. It now stores a base max health value to allow for
/// safe, non-exponential modification by other systems like AdaptiveFormController.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Runtime Enemy Stats")]
    public string EnemyId { get; private set; }
    public string EnemyType { get; private set; }
    [HideInInspector] public float currentHealth;
    [HideInInspector] public float maxHealth;
    [HideInInspector] public float xpValue;
    
    // --- Private State ---
    public float baseMaxHealth;
    private Rigidbody2D rb;
    private KafkaClient kafkaClient;
    private string lastAttackingPlayerId;
    private EnemyData enemyData;
    
    // --- Static Events ---
    /// <summary>
    /// Event fired when an enemy takes damage.
    /// Parameters: Damage Amount (float), World Position (Vector3) for visual effects.
    /// </summary>
    public static event Action<float, Vector3> OnDamaged;

    /// <summary>
    /// Event fired when an enemy is defeated, passing its full data object.
    /// This is the single source of truth for enemy death events.
    /// </summary>
    public static event Action<EnemyData> OnEnemyDefeated;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        kafkaClient = KafkaClient.Instance;
        EnemyId = $"enemy_{GetInstanceID()}";
    }

    public void Initialize(EnemyData data)
    {
        this.enemyData = data;
        EnemyType = data.enemyID;
        this.baseMaxHealth = data.maxHealth; // Store the original base value.
        this.maxHealth = data.maxHealth;     // This is the current max health, which can be modified.
        this.currentHealth = data.maxHealth;
        this.xpValue = data.xpValue;
    }

    /// <summary>
    /// Applies a multiplier to the enemy's base health. This is the safe way to modify health.
    /// </summary>
    /// <param name="multiplier">The factor to multiply health by (e.g., 2.0 for Juggernaut, 1.0 to reset).</param>
    public void ApplyHealthMultiplier(float multiplier)
    {
        // 1. Preserve the current health percentage.
        float healthPercent = (maxHealth > 0) ? currentHealth / maxHealth : 1f;

        // 2. Calculate the new max health from the ORIGINAL base value. This prevents exponential growth.
        this.maxHealth = this.baseMaxHealth * multiplier;

        // 3. Set the current health to the same percentage of the new max health. This preserves the "chip away" feel.
        this.currentHealth = this.maxHealth * healthPercent;
    }

    public void TakeDamage(float amount, string sourceWeaponId, bool isProjectile, string attackerPlayerId)
    {
        if (!enabled || currentHealth <= 0) return;

        this.lastAttackingPlayerId = attackerPlayerId;

        currentHealth -= amount;
        OnDamaged?.Invoke(amount, transform.position);

        SendDamageTakenEvent(amount, sourceWeaponId, isProjectile);

        if (currentHealth <= 0)
        {
            Die(sourceWeaponId);
        }
    }

    private void Die(string killingWeaponId)
    {
        StatisticsTracker.RecordEnemyKilled(this.EnemyType);
        OnEnemyDefeated?.Invoke(this.enemyData);
        SendEnemyDeathEvent(killingWeaponId);
        enabled = false;
        Destroy(gameObject);
    }
    private void SendDamageTakenEvent(float dmgAmount, string weaponId, bool isProjectile)
    {
        if (kafkaClient == null) return;
        var payload = new Dictionary<string, object>
        {
            { "dmg_amount", dmgAmount },
            { "enemy_id", EnemyId },
            { "enemy_type", EnemyType },
            { "source_weapon_id", weaponId },
            { "is_projectile", isProjectile }
        };
        kafkaClient.SendGameplayEvent("damage_taken_event", this.lastAttackingPlayerId, payload);
    }

    private void SendEnemyDeathEvent(string killingWeaponId)
    {
        if (kafkaClient == null) return;

        // Get the enemy's final velocity vector at the moment of death.
        Vector2 finalVelocity = rb.linearVelocity;

        var payload = new Dictionary<string, object>
        {
            { "enemy_id", EnemyId },
            { "enemy_type", EnemyType },
            { "killed_by_weapon_id", killingWeaponId },
            { "position", new Dictionary<string, float> { { "x", transform.position.x }, { "y", transform.position.y } } },
            { "velocity", new Dictionary<string, float> { { "vx", finalVelocity.x }, { "vy", finalVelocity.y } } }
        };
        kafkaClient.SendGameplayEvent("enemy_death_event", this.lastAttackingPlayerId, payload);
    }
}
