// GameClient/Assets/Scripts/Player/PlayerAttack.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the player's auto-attack mechanism based on data from a WeaponData ScriptableObject.
/// It periodically finds and attacks the nearest enemy, sending a 'weapon_hit_event' to Kafka.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    private string playerId;
    public WeaponData currentWeapon { get; private set; }

    private float attackInterval;
    private float attackRange;
    private float baseDamage;
    private float attackTimer;
    private float currentDamage;

    private KafkaClient kafkaClient;

    // The Initialize method, called by PlayerInitializer
    public void Initialize(CharacterData data)
    {
        this.playerId = data.characterName;
        this.currentWeapon = data.startingWeapon;
        this.currentDamage = data.baseDamage; // Initialize from character

        if (this.currentWeapon != null)
        {
            InitializeWeaponStats();
        }
        else
        {
            Debug.LogError("PlayerAttack: CharacterData has no Starting Weapon assigned!", this);
            enabled = false;
        }
    }

    void Awake()
    {
        kafkaClient = FindObjectOfType<KafkaClient>();
    }


    /// <summary>
    /// Sets the component's internal stats from the assigned WeaponData asset.
    /// This allows for stats to be changed at runtime by swapping WeaponData assets if needed.
    /// </summary>
    public void InitializeWeaponStats()
    {
        // character's base damage as the foundation, and the weapon's stats for everything else.
        attackInterval = currentWeapon.attackInterval;
        attackRange = currentWeapon.attackRange;
        // The baseDamage field in this script is now initialized from CharacterData
        attackTimer = attackInterval;
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
    /// Increases the player's damage by a flat amount or a percentage.
    /// </summary>
    public void IncreaseDamage(float value, bool isPercentage)
    {
        if (isPercentage)
        {
            currentDamage *= (1 + value); // 0.1 = +10%
        }
        else
        {
            currentDamage += value; // Flat increase
        }
    }

    /// <summary>
    /// Performs the auto-attack: finds target, deals damage, and sends a Kafka event.
    /// </summary>
    private void PerformAttack()
    {
        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            EnemyHealth enemyHealth = nearestEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Use the component's currentDamage, which was set from CharacterData and can be upgraded.
                enemyHealth.TakeDamage(this.currentDamage, currentWeapon.weaponID);

                // Send the event to Kafka
                SendWeaponHitEvent(this.currentDamage, enemyHealth.EnemyId);
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
        float minDistanceSqr = attackRange * attackRange;
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
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
