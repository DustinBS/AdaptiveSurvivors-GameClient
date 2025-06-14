// GameClient/Assets/Scripts/Enemy/EnemyMovement.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    private Transform player;
    private float moveSpeed;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Find the player in the scene by their tag. This is more robust
        // than passing the transform from the spawner.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("EnemyMovement: Could not find GameObject with 'Player' tag.", this);
        }
    }

    /// <summary>
    /// Initializes the enemy's movement properties from its data asset.
    /// </summary>
    /// <param name="data">The EnemyData asset containing stats for this enemy.</param>
    public void Initialize(EnemyData data)
    {
        moveSpeed = data.moveSpeed;
    }

    void FixedUpdate()
    {
        if (player != null)
        {
            // Calculate direction towards the player
            Vector2 direction = (player.position - transform.position).normalized;
            // Apply velocity to move towards the player
            rb.linearVelocity = direction * moveSpeed;
        }
    }
}
