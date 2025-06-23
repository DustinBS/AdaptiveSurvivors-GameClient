// GameClient/Assets/Scripts/Player/PlayerAttack.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages player attacks. Now handles both melee and projectile weapons,
/// and sends enriched event data to Kafka.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    private string playerId;
    public WeaponData currentWeapon { get; private set; }
    private float attackTimer;
    private float currentDamage;
    private KafkaClient kafkaClient;

    public void Initialize(CharacterData data)
    {
        this.playerId = data.characterName;
        this.currentWeapon = data.startingWeapon;
        this.currentDamage = data.baseDamage;

        if (this.currentWeapon != null)
        {
            attackTimer = currentWeapon.attackInterval;
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

    void Update()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            PerformAttack();
            attackTimer = currentWeapon.attackInterval;
        }
    }

    public void IncreaseDamage(float value, bool isPercentage)
    {
        currentDamage *= (isPercentage) ? (1 + value) : 1;
        currentDamage += (isPercentage) ? 0 : value;
    }

    private void PerformAttack()
    {
        GameObject nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null) return;

        if (currentWeapon.isProjectile)
        {
            // --- PROJECTILE LOGIC ---
            if (currentWeapon.projectilePrefab == null)
            {
                Debug.LogError($"Weapon '{currentWeapon.name}' is projectile but has no prefab!", currentWeapon);
                return;
            }
            Vector2 direction = (nearestEnemy.transform.position - transform.position).normalized;
            Projectile projectile = Instantiate(currentWeapon.projectilePrefab, transform.position, Quaternion.identity).GetComponent<Projectile>();
            projectile.Initialize(direction, this.currentDamage, currentWeapon.isProjectile);
        }
        else
        {
            // --- MELEE LOGIC ---
            if (nearestEnemy.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                // Directly damage the enemy and pass the 'isProjectile' flag.
                enemyHealth.TakeDamage(this.currentDamage, currentWeapon.weaponID, currentWeapon.isProjectile);
            }
        }
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        GameObject nearest = null;
        float minDistanceSqr = currentWeapon.attackRange * currentWeapon.attackRange;

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (currentWeapon != null)
        {
            Gizmos.DrawWireSphere(transform.position, currentWeapon.attackRange);
        }
    }
}
