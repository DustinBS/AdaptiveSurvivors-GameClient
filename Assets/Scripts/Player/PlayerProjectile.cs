// GameClient/Assets/Scripts/Player/PlayerProjectile.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls the behavior of a projectile fired by the player.
/// It moves in a set direction, deals damage on impact with an enemy,
/// and destroys itself after a set time or on impact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("The speed at which the projectile travels.")]
    [SerializeField] private float speed = 20f;
    [Tooltip("How long the projectile exists in seconds before being destroyed.")]
    [SerializeField] private float lifetime = 3f;
    [Tooltip("Visual effect to instantiate upon impact.")]
    [SerializeField] private GameObject impactVFX;

    private float damage;
    private bool isProjectileFlag;
    private Rigidbody2D rb;
    private KafkaClient kafkaClient;
    private string playerId;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ensure the projectile's collider is a trigger so it doesn't physically push enemies.
        GetComponent<Collider2D>().isTrigger = true;
        kafkaClient = KafkaClient.Instance;
    }

    /// <summary>
    /// Initializes the projectile with its damage, direction, and damage type.
    /// </summary>
    public void Initialize(Vector2 direction, float projDamage, bool isProj, string ownerPlayerId)
    {
        this.damage = projDamage;
        this.isProjectileFlag = isProj;
        this.playerId = ownerPlayerId; // Set the ID from the creator

        // Set the projectile in motion
        rb.linearVelocity = direction.normalized * speed;

        // Destroy the projectile after its lifetime expires to prevent scene clutter.
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collisions with non-enemy objects
        if (!other.CompareTag("Enemy")) return;

        if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.TakeDamage(this.damage, "projectile_hit", this.isProjectileFlag, this.playerId);
            SendDamageDealtEvent(this.damage, enemyHealth.EnemyType);
        }

        // Create a visual effect at the impact point, if one is assigned.
        if (impactVFX != null)
        {
            Instantiate(impactVFX, transform.position, Quaternion.identity);
        }

        // Destroy the projectile on impact.
        Destroy(gameObject);
    }

    private void SendDamageDealtEvent(float damageAmount, string enemyType)
    {
        if (kafkaClient == null) return;
        var payload = new Dictionary<string, object>
        {
            { "dmg_amount", damageAmount },
            { "enemy_type", enemyType },
            { "is_projectile", true }
        };
        kafkaClient.SendGameplayEvent("player_damage_dealt_event", this.playerId, payload);
    }
}
