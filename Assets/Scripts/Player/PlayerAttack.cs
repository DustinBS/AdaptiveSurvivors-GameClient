// GameClient/Assets/Scripts/Player/PlayerAttack.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages player attacks. Now handles both melee and projectile weapons,
/// and sends enriched event data to Kafka. It reads all combat stats from the central PlayerStats component.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("A reference to the AttributeRegistry asset. Used to access specific attribute data.")]
    [SerializeField] private AttributeRegistry attributeRegistry;

    // --- Component & Data References ---
    private PlayerStats playerStats;
    private KafkaClient kafkaClient;

    // --- State ---
    public WeaponData currentWeapon { get; private set; }
    private float attackTimer;

    /// <summary>
    /// This is now only responsible for setting the weapon based on character data.
    /// All stats are managed by PlayerStats.
    /// </summary>
    public void Initialize(CharacterData data)
    {
        this.currentWeapon = data.startingWeapon;

        if (this.currentWeapon == null)
        {
            Debug.LogError("PlayerAttack: CharacterData has no Starting Weapon assigned!", this);
            enabled = false;
        }
    }

    void Awake()
    {
        // Get references to the other components on this GameObject.
        playerStats = GetComponent<PlayerStats>();
        kafkaClient = FindFirstObjectByType<KafkaClient>(); // This can be slow, consider a singleton or service locator pattern later.
    }

    void Start()
    {
        // Set the initial attack timer based on the starting weapon's stats.
        if(currentWeapon != null)
        {
            attackTimer = currentWeapon.attacksPerSecond;
        }
    }

void Update()
{
    attackTimer -= Time.deltaTime;
    if (attackTimer <= 0)
    {
        PerformAttack();

        // --- Attack Speed & Interval Calculation ---
        float finalAttacksPerSecond = playerStats.GetComposedStatValue(
            attributeRegistry.BaseAttackSpeed,
            attributeRegistry.CharacterAttackSpeedMultiplier,
            attributeRegistry.GlobalAttackSpeedMultiplier
        );

        // The interval is the inverse of attacks per second. Prevent division by zero.
        float finalInterval = (finalAttacksPerSecond > 0) ? 1f / finalAttacksPerSecond : float.MaxValue;

        attackTimer = finalInterval;
    }
}

    private void PerformAttack()
    {
        if (currentWeapon == null) return;

        float currentAttackRange = playerStats.GetAttributeValue(attributeRegistry.AttackRange);
        GameObject nearestEnemy = FindNearestEnemy(currentAttackRange);
        if (nearestEnemy == null) return;

        // --- Damage Calculation ---
        float finalDamage = playerStats.GetComposedStatValue(
            attributeRegistry.BaseDamage,
            attributeRegistry.CharacterDamageMultiplier,
            attributeRegistry.GlobalDamageMultiplier
        );

        if (currentWeapon.isProjectile)

        {
            // --- PROJECTILE LOGIC ---
            if (currentWeapon.projectilePrefab == null)
            {
                Debug.LogError($"Weapon '{currentWeapon.name}' is projectile but has no prefab!", currentWeapon);
                return;
            }
            Vector2 direction = (nearestEnemy.transform.position - transform.position).normalized;
            PlayerProjectile projectile = Instantiate(currentWeapon.projectilePrefab, transform.position, Quaternion.identity).GetComponent<PlayerProjectile>();

            // Pass the calculated damage and the player's ID to the projectile.
            projectile.Initialize(direction, finalDamage, currentWeapon.isProjectile, playerStats.playerID);
        }
        else
        {
            // --- MELEE LOGIC ---
            if (nearestEnemy.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                // Directly damage the enemy, passing the player's ID from PlayerStats.
                enemyHealth.TakeDamage(finalDamage, currentWeapon.weaponID, currentWeapon.isProjectile, playerStats.playerID);
                SendDamageDealtEvent(finalDamage, enemyHealth.EnemyType);
            }
        }
    }

    private GameObject FindNearestEnemy(float attackRange)
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

    private void SendDamageDealtEvent(float damageAmount, string enemyType)
    {
        if (kafkaClient == null) return;
        var payload = new Dictionary<string, object>
        {
            { "dmg_amount", damageAmount },
            { "enemy_type", enemyType },
            { "is_projectile", false }
        };
        // Get the player's ID from PlayerStats.
        kafkaClient.SendGameplayEvent("player_damage_dealt_event", playerStats.playerID, payload);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (playerStats != null && attributeRegistry != null)
        {
            // Draw the gizmo using the live attack range value from PlayerStats.
            Gizmos.DrawWireSphere(transform.position, playerStats.GetAttributeValue(attributeRegistry.AttackRange));
        }
        else if (currentWeapon != null)
        {
            // Fallback for when not in play mode.
            Gizmos.DrawWireSphere(transform.position, currentWeapon.attackRange);
        }
    }
}