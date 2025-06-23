// GameClient/Assets/Scripts/Enemy/EnemyHealth.cs

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages the health of an enemy. Now calculates damage after checking for
/// resistances from an associated AdaptiveEnemy component.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : MonoBehaviour
{
    // --- Public Fields ---
    [Header("Runtime Enemy Stats")]
    public string EnemyId { get; private set; }
    public string EnemyType { get; private set; }
    public float currentHealth;
    public float maxHealth;
    public float xpValue;
    private Rigidbody2D rb;

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
    private AdaptiveEnemy adaptiveComponent; // Cached reference


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        kafkaClient = KafkaClient.Instance;
        EnemyId = $"enemy_{GetInstanceID()}";
        TryGetComponent(out adaptiveComponent);
    }

    public void Initialize(EnemyData data)
    {
        EnemyType = data.enemyID;
        maxHealth = data.maxHealth;
        currentHealth = data.maxHealth;
        xpValue = data.xpValue;
    }

    public void TakeDamage(float amount, string sourceWeaponId, bool isProjectile)
    {
        if (!enabled || currentHealth <= 0) return;

        float finalDamage = amount;

        currentHealth -= finalDamage;
        OnDamaged?.Invoke(finalDamage, transform.position);

        SendDamageTakenEvent(finalDamage, sourceWeaponId, isProjectile);

        if (currentHealth <= 0)
        {
            Die(sourceWeaponId);
        }
    }

    private void Die(string killingWeaponId)
    {
        OnEnemyDeath?.Invoke(EnemyId, EnemyType, xpValue);
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
        kafkaClient.SendGameplayEvent("damage_taken_event", playerId, payload);
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
        kafkaClient.SendGameplayEvent("enemy_death_event", playerId, payload);
    }
}
