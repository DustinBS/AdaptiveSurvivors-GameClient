// GameClient/Assets/Scripts/Enemy/EnemyMovement.cs

using UnityEngine;

/// <summary>
/// Handles the movement of an enemy towards a designated target (the player).
/// Requires a Rigidbody2D component on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    private Transform playerTarget;
    private float moveSpeed;
    private Rigidbody2D rb;
    private bool isInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Initializes the movement component with its target and speed.
    /// This method is called by the EnemySpawner to provide dependencies,
    /// avoiding expensive FindObject operations.
    /// </summary>
    /// <param name="target">The Transform of the player to move towards.</param>
    /// <param name="speed">The movement speed, sourced from EnemyData.</param>
    public void Initialize(Transform target, float speed)
    {
        playerTarget = target;
        moveSpeed = speed;
        isInitialized = true;
    }

    void FixedUpdate()
    {
        // Do not execute movement logic until the component has been initialized.
        if (!isInitialized || playerTarget == null)
        {
            return;
        }

        // Calculate the direction vector from the enemy to the player.
        Vector2 direction = (playerTarget.position - transform.position).normalized;

        // Apply velocity to the Rigidbody2D to move the enemy.
        rb.linearVelocity = direction * moveSpeed;
    }
}
