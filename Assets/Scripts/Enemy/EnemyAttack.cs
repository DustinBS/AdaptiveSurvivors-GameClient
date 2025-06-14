// GameClient/Assets/Scripts/Enemy/EnemyAttack.cs
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    // [FIX] These are now public but with a [HideInInspector] attribute.
    // This means other scripts can access them, but they won't clutter the Inspector
    // since they are set dynamically by the EnemySpawner.
    [HideInInspector] public float attackDamage;
    [HideInInspector] public float attackCooldown;

    private float lastAttackTime;
    private PlayerStatus targetPlayer;

    /// <summary>
    /// Initializes the enemy's attack stats from the EnemyData asset.
    /// This method is called by the EnemySpawner when the enemy is created.
    /// </summary>
    public void Initialize(EnemyData enemyData)
    {
        attackDamage = enemyData.damage;
        attackCooldown = 1f / enemyData.attackSpeed; // Convert attack speed (attacks/sec) to cooldown
    }

    private void Update()
    {
        if (targetPlayer != null && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack(targetPlayer);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetPlayer = other.GetComponent<PlayerStatus>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetPlayer = null;
        }
    }

    private void Attack(PlayerStatus player)
    {
        player.TakeDamage(attackDamage);
        lastAttackTime = Time.time;
    }
}
