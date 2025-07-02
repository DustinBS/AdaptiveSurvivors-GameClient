// GameClient/Assets/Scripts/Data/EnemyData.cs

using UnityEngine;

/// <summary>
/// Defines the static properties of an enemy type using a ScriptableObject.
/// This has been refactored to use the Strategy Pattern, referencing AI behaviors
/// instead of holding direct stat values like speed and damage.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Adaptive Survivors/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Core Identification")]
    [Tooltip("A unique identifier for this enemy type (e.g., 'goblin_grunt', 'orc_brute'). Used for backend event tracking.")]
    public string enemyID;

    [Tooltip("Display name for the enemy, used in UI or logs.")]
    public string enemyName;

    [Header("Core Stats")]
    [Tooltip("The base health of the enemy.")]
    public float maxHealth = 100f;

    [Tooltip("Experience points granted to the player upon defeating this enemy.")]
    public float xpValue = 10f;

    [Header("AI Behavior (Strategy Pattern)")]
    [Tooltip("The movement behavior asset for this enemy.")]
    public MovementStrategy movementStrategy;

    [Tooltip("The attack behavior asset for this enemy.")]
    public AttackStrategy attackStrategy;

    [Header("Visuals")]
    [Tooltip("The prefab containing the enemy's visuals and core components like EnemyBrain and EnemyHealth.")]
    public GameObject visualPrefab;

    [Header("Behavioral Flags")]
    [Tooltip("If true, this enemy does not need to be killed for no enemy checks for events like the Seer encounter to begin. It will be despawned automatically.")]
    public bool isExemptFromClearanceChecks = false;
}