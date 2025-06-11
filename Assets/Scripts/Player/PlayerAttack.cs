// GameClient/Assets/Scripts/Player/PlayerAttack.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the player's auto-attack mechanism based on data from a WeaponData ScriptableObject.
/// It periodically finds and attacks the nearest enemy, sending a 'weapon_hit_event' to Kafka.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Weapon Configuration")]
    [Tooltip("The ScriptableObject that defines the properties of the currently equipped weapon.")]
    public WeaponData currentWeapon;

    [Header("Dependencies")]
    [Tooltip("Unique identifier for the player. Should match other player scripts.")]
    public string playerId = "player_001";

    // Private fields to hold runtime stats derived from WeaponData
    private float attackInterval;
    private float attackRange;
    private float baseDamage;
    private float attackTimer;

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    void Awake()
    {
        kafkaClient = FindObjectOfType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("PlayerAttack: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
            return; // Stop execution if KafkaClient is missing
        }

        // Initialize attack properties from the ScriptableObject
        if (currentWeapon != null)
        {
            InitializeWeaponStats();
        }
        else
        {
            Debug.LogError("PlayerAttack: No WeaponData assigned. Please assign a WeaponData asset in the Inspector.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Sets the component's internal stats from the assigned WeaponData asset.
    /// This allows for stats to be changed at runtime by swapping WeaponData assets if needed.
    /// </summary>
    public void InitializeWeaponStats()
    {
        attackInterval = currentWeapon.attackInterval;
        attackRange = currentWeapon.attackRange;
        baseDamage = currentWeapon.baseDamage;
        attackTimer = attackInterval; // Set timer to attack immediately on start
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0)
        {
            PerformAttack();
            attackTimer = attackInterval; // Reset timer
        }
    }

    /// <summary>
    /// Performs the auto-attack: finds target, deals damage, and sends Kafka event.
    /// </summary>
    private void PerformAttack()
    {
        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            EnemyHealth enemyHealth = nearestEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // For now, damage is simple. This could be expanded with critical hits, etc.
                float actualDamageDealt = baseDamage;
                enemyHealth.TakeDamage(actualDamageDealt, currentWeapon.weaponID);

                // Send the event to Kafka
                SendWeaponHitEvent(actualDamageDealt, enemyHealth.EnemyId);
            }
        }
    }

    /// <summary>
    /// Finds the nearest GameObject tagged "Enemy" within the attack range.
    /// </summary>
    /// <returns>The nearest enemy GameObject, or null if no enemy is found.</returns>
    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        GameObject nearest = null;
        float minDistanceSqr = attackRange * attackRange; // Use squared distance for efficiency

        foreach (GameObject enemy in enemies)
        {
            float distanceSqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                nearest = enemy;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Sends a weapon_hit_event to Kafka.
    /// </summary>
    /// <param name="dmgDealt">The amount of damage dealt.</param>
    /// <param name="enemyId">The ID of the enemy that was hit.</param>
    private void SendWeaponHitEvent(float dmgDealt, string enemyId)
    {
        var payload = new Dictionary<string, object>
        {
            { "weapon_id", currentWeapon.weaponID },
            { "dmg_dealt", dmgDealt },
            { "enemy_id", enemyId }
        };
        kafkaClient.SendGameplayEvent("weapon_hit_event", playerId, payload);
    }

    // Optional: Draw attack range in editor for visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
