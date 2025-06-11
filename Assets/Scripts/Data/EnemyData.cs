// GameClient/Assets/Scripts/Data/EnemyData.cs

using UnityEngine;

/// <summary>
/// Defines the static properties of an enemy type using a ScriptableObject.
/// This allows for creating and managing different enemy types as assets in the project.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Adaptive Survivors/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Core Identification")]
    [Tooltip("A unique identifier for this enemy type (e.g., 'goblin_grunt', 'orc_brute'). Used for backend event tracking.")]
    public string enemyID;

    [Tooltip("Display name for the enemy, used in UI or logs.")]
    public string enemyName;

    [Header("Gameplay Stats")]
    [Tooltip("The base health of the enemy.")]
    public float maxHealth = 100f;

    [Tooltip("How fast the enemy moves towards the player.")]
    public float moveSpeed = 3f;

    [Tooltip("Damage dealt to the player on contact.")]
    public float baseDamage = 10f;

    [Tooltip("Experience points granted to the player upon defeating this enemy.")]
    public float xpValue = 10f;

    [Header("Visuals")]
    [Tooltip("The prefab containing the enemy's visuals, Rigidbody2D, colliders, and scripts like EnemyHealth.")]
    public GameObject visualPrefab;
}
