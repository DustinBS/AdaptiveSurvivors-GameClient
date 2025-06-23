// GameClient/Assets/Scripts/Player/Projectile.cs

using UnityEngine;

/// <summary>
/// Controls the behavior of a projectile fired by the player.
/// It moves in a set direction, deals damage on impact with an enemy,
/// and destroys itself after a set time or on impact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ensure the projectile's collider is a trigger so it doesn't physically push enemies.
        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>
    /// Initializes the projectile with its damage, direction, and damage type.
    /// </summary>
    public void Initialize(Vector2 direction, float projDamage, bool isProj)
    {
        this.damage = projDamage;
        this.isProjectileFlag = isProj;

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
            // The weaponID is no longer strictly necessary here, but we pass it for consistency.
            // The isProjectileFlag is the crucial piece of information.
            enemyHealth.TakeDamage(this.damage, "projectile_hit", this.isProjectileFlag);
        }

        // Create a visual effect at the impact point, if one is assigned.
        if (impactVFX != null)
        {
            Instantiate(impactVFX, transform.position, Quaternion.identity);
        }

        // Destroy the projectile on impact.
        Destroy(gameObject);
    }
}
