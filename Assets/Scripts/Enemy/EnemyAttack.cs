// GameClient/Assets/Scripts/Enemy/EnemyAttack.cs

using UnityEngine;

/// <summary>
/// Handles the enemy's melee attack, which occurs on collision with the player.
/// This script requires a Collider2D on the same GameObject to detect collisions.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    private float damage;
    private bool isInitialized = false;

    /// <summary>
    /// Initializes the attack component with the damage value from EnemyData.
    /// Called by the EnemySpawner upon instantiation.
    /// </summary>
    /// <param name="damageAmount">The damage to deal on contact.</param>
    public void Initialize(float damageAmount)
    {
        damage = damageAmount;
        isInitialized = true;
    }

    /// <summary>
    /// This built-in Unity function is called automatically when this object's
    /// collider makes contact with another solid collider.
    /// </summary>
    /// <param name="collision">Information about the collision event.</param>
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Do nothing if the component hasn't been initialized with a damage value.
        if (!isInitialized)
        {
            return;
        }

        // Check if the object we collided with has the "Player" tag.
        if (collision.gameObject.CompareTag("Player"))
        {
            // Try to get the PlayerStatus component from the collided object.
            PlayerStatus playerStatus = collision.gameObject.GetComponent<PlayerStatus>();
            if (playerStatus != null)
            {
                // Call the TakeDamage method on the player.
                playerStatus.TakeDamage(damage, gameObject.name);
            }
        }
    }
}
