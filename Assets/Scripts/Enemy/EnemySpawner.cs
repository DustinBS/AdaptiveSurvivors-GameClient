// GameClient/Assets/Scripts/Enemy/EnemySpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the spawning of enemies based on EnemyData ScriptableObjects.
/// It instantiates enemy prefabs and then initializes their stats (health, speed, etc.)
/// from the corresponding data asset.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("List of regular 'fodder' enemy types this spawner can instantiate.")]
    public List<EnemyData> fodderEnemies;

    [Tooltip("List of 'elite' enemy types this spawner can instantiate.")]
    public List<EnemyData> eliteEnemies;

    [Tooltip("The interval (in seconds) between spawn attempts.")]
    public float spawnInterval = 1.0f;

    [Tooltip("The maximum number of enemies allowed on screen at once.")]
    public int maxEnemiesOnScreen = 50;

    [Tooltip("The radius around the player where enemies can spawn.")]
    public float spawnRadius = 15f;

    [Tooltip("The minimum distance from the player to spawn enemies, to avoid spawning on top of them.")]
    public float minSpawnDistanceFromPlayer = 10f;

    [Header("Elite Spawning Logic")]
    [Tooltip("The wave number at which elites can start appearing.")]
    public int eliteStartWave = 3;
    [Tooltip("The chance (0 to 1) to spawn an elite instead of a regular enemy, if conditions are met.")]
    [Range(0f, 1f)]
    public float eliteSpawnChance = 0.1f;

    private float spawnTimer;
    private Transform playerTransform;

    void Awake()
    {
        // Find the player's transform for spawn position calculations.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("EnemySpawner: Player GameObject not found. Ensure player is tagged 'Player'.", this);
            enabled = false;
        }
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0)
        {
            // Check the current enemy count against the max limit.
            // Note: FindGameObjectsWithTag can be slow. For performance, a manager could track the count.
            if (GameObject.FindGameObjectsWithTag("Enemy").Length < maxEnemiesOnScreen)
            {
                SpawnEnemy();
            }
            spawnTimer = spawnInterval; // Reset timer regardless of spawn success
        }
    }

    /// <summary>
    /// Chooses an enemy type, finds a valid position, instantiates, and initializes it.
    /// </summary>
    private void SpawnEnemy()
    {
        EnemyData enemyToSpawnData = ChooseEnemyType();
        if (enemyToSpawnData == null) return; // No valid enemy type chosen

        Vector3 spawnPosition = GetRandomSpawnPosition();
        if (spawnPosition == Vector3.zero) return; // No valid spawn position found

        // 1. Instantiate the prefab defined in the EnemyData
        GameObject enemyInstance = Instantiate(enemyToSpawnData.visualPrefab, spawnPosition, Quaternion.identity);

        // 2. Get the EnemyHealth component from the new instance
        EnemyHealth enemyHealth = enemyInstance.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // 3. Initialize the enemy's stats using the data from the ScriptableObject
            enemyHealth.Initialize(enemyToSpawnData);
        }
        else
        {
            Debug.LogError($"Enemy prefab for '{enemyToSpawnData.enemyName}' is missing an EnemyHealth component.", enemyInstance);
        }

        // You would also initialize an EnemyMovement script here if it needed data (e.g., speed)
        // var enemyMovement = enemyInstance.GetComponent<EnemyMovement>();
        // if (enemyMovement != null) enemyMovement.Initialize(enemyToSpawnData);
    }

    /// <summary>
    /// Decides whether to spawn a fodder or an elite enemy.
    /// </summary>
    /// <returns>The EnemyData for the chosen enemy type.</returns>
    private EnemyData ChooseEnemyType()
    {
        bool canSpawnElite = GameManager.Instance != null &&
                             GameManager.Instance.currentWave >= eliteStartWave &&
                             eliteEnemies.Any() &&
                             Random.value < eliteSpawnChance;

        if (canSpawnElite)
        {
            return eliteEnemies[Random.Range(0, eliteEnemies.Count)];
        }

        // Default to spawning a fodder enemy
        if (fodderEnemies.Any())
        {
            return fodderEnemies[Random.Range(0, fodderEnemies.Count)];
        }

        Debug.LogWarning("EnemySpawner: No fodder enemies available to spawn.");
        return null;
    }

    /// <summary>
    /// Calculates a random spawn position around the player.
    /// </summary>
    /// <returns>A valid spawn position or Vector3.zero if none is found.</returns>
    private Vector3 GetRandomSpawnPosition()
    {
        int attempts = 0;
        while (attempts < 10)
        {
            // Get a random point on the edge of a circle for predictable spawning off-screen
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = playerTransform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * spawnRadius;

            // Optional: Check if position is too close (shouldn't happen with this logic, but good practice)
            if (Vector3.Distance(spawnPos, playerTransform.position) >= minSpawnDistanceFromPlayer)
            {
                return spawnPos;
            }
            attempts++;
        }
        return Vector3.zero; // Failed to find a valid position
    }

    // Visualize the spawn radii in the editor
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistanceFromPlayer);
    }
}
