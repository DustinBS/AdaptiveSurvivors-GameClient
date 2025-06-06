// GameClient/Assets/Scripts/Player/PlayerAttack.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For OrderByDescending

// This script manages the player's auto-attack mechanism.
// It will periodically find and attack nearby enemies, sending weapon_hit_event to Kafka.
// It assumes the presence of a KafkaClient in the scene and EnemyHealth components on enemies.

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("The time interval between attacks in seconds.")]
    public float attackInterval = 1.0f;

    [Tooltip("The range within which the player can detect and attack enemies.")]
    public float attackRange = 5.0f;

    [Tooltip("The base damage dealt by the player's auto-attack.")]
    public float baseDamage = 10f;

    [Tooltip("The unique ID of this weapon (e.g., 'starting_sword', 'magic_wand').")]
    public string weaponId = "player_auto_attack";

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    // Player ID for Kafka events
    [Tooltip("Unique identifier for this player.")]
    public string playerId = "player_001"; // Should match PlayerMovement's ID

    private float attackTimer;

    void Awake()
    {
        // Updated to use FindAnyObjectByType to resolve deprecation warning
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("PlayerAttack: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
        }

        attackTimer = attackInterval; // Initialize timer to attack immediately on start
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
        // Step 1: Find the nearest enemy within attack range.
        // For a more robust solution, use Physics.OverlapCircleAll or similar
        // with a specific enemy layer to avoid hitting non-enemies.
        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            // Step 2: Get the EnemyHealth component from the target.
            EnemyHealth enemyHealth = nearestEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Step 3: Apply damage to the enemy.
                // In a real game, this might involve crit chance, damage modifiers, etc.
                float actualDamageDealt = baseDamage;
                enemyHealth.TakeDamage(actualDamageDealt, "player_attack");

                // Step 4: Send weapon_hit_event to Kafka.
                SendWeaponHitEvent(actualDamageDealt, enemyHealth.EnemyId);

                Debug.Log($"Player attacked enemy '{enemyHealth.EnemyId}' dealing {actualDamageDealt} damage.");
            }
            else
            {
                Debug.LogWarning($"PlayerAttack: Found enemy '{nearestEnemy.name}' but no EnemyHealth component.", nearestEnemy);
            }
        }
        // else
        // {
        //     Debug.Log("No enemy found within attack range.");
        // }
    }

    /// <summary>
    /// Finds the nearest GameObject tagged "Enemy" within the attack range.
    /// This is a simple implementation and can be optimized for performance (e.g., using object pooling, spatial partitioning).
    /// </summary>
    /// <returns>The nearest enemy GameObject, or null if no enemy is found.</returns>
    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= attackRange && distance < minDistance)
            {
                minDistance = distance;
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
            { "weapon_id", weaponId },
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
