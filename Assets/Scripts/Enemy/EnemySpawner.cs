// GameClient/Assets/Scripts/Enemy/EnemySpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For OrderByDescending

// This script manages the spawning of enemies in the game.
// It can spawn regular fodder enemies and potentially adaptive elite enemies.
// It should be placed in the scene, perhaps attached to an empty GameObject.

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("List of enemy prefabs that this spawner can instantiate.")]
    public List<GameObject> enemyPrefabs;

    [Tooltip("The interval (in seconds) between enemy spawns.")]
    public float spawnInterval = 2.0f;

    [Tooltip("The maximum number of enemies allowed on screen at once.")]
    public int maxEnemiesOnScreen = 20;

    [Tooltip("The radius around the spawner where enemies can appear.")]
    public float spawnRadius = 10f;

    [Tooltip("The minimum distance from the player to spawn enemies, to avoid spawning on top of the player.")]
    public float minSpawnDistanceFromPlayer = 5f;

    [Tooltip("The current number of enemies active in the scene.")]
    public int currentEnemyCount = 0;

    [Header("Elite Enemy Settings")]
    [Tooltip("The interval (in waves) at which an Elite enemy should spawn.")]
    public int eliteSpawnWaveInterval = 3;

    [Tooltip("Prefab for the Adaptive Elite enemy.")]
    public GameObject adaptiveElitePrefab;

    private float spawnTimer;
    private Transform playerTransform; // Reference to the player's transform

    void Awake()
    {
        spawnTimer = spawnInterval;
        GameObject player = GameObject.FindGameObjectWithTag("Player"); // Assuming player is tagged "Player"
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("EnemySpawner: Player GameObject not found. Ensure your player is tagged 'Player'.", this);
            enabled = false;
        }

        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogError("EnemySpawner: No enemy prefabs assigned. Please assign enemy prefabs in the Inspector.", this);
            enabled = false;
        }
    }

    void Update()
    {
        currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0)
        {
            TrySpawnEnemy();
            spawnTimer = spawnInterval; // Reset timer
        }
    }

    /// <summary>
    /// Attempts to spawn an enemy if the current enemy count is below the maximum.
    /// Decides whether to spawn a regular enemy or an elite based on game state.
    /// </summary>
    private void TrySpawnEnemy()
    {
        if (currentEnemyCount >= maxEnemiesOnScreen)
        {
            // Debug.Log("Max enemies reached, skipping spawn.");
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();
        if (spawnPosition == Vector3.zero) // If no valid spawn position found
        {
            // Debug.Log("No valid spawn position found, skipping spawn.");
            return;
        }

        GameObject enemyToSpawn = null;
        bool isEliteSpawn = false;

        // Check if it's time to spawn an Elite enemy
        if (GameManager.Instance != null && GameManager.Instance.currentWave % eliteSpawnWaveInterval == 0 && adaptiveElitePrefab != null)
        {
            // Ensure Elite hasn't just spawned or has a cooldown
            // This logic can be more sophisticated (e.g., only one elite per wave, or after a certain time)
            if (GameObject.FindGameObjectsWithTag("Enemy").Count(e => e.GetComponent<AdaptiveEnemy>() != null) == 0) // Only one elite at a time
            {
                enemyToSpawn = adaptiveElitePrefab;
                isEliteSpawn = true;
                Debug.Log($"Spawning Adaptive Elite at wave {GameManager.Instance.currentWave}!");
            }
        }

        // If not an elite spawn, choose a random regular enemy
        if (enemyToSpawn == null && enemyPrefabs.Count > 0)
        {
            int randomIndex = Random.Range(0, enemyPrefabs.Count);
            enemyToSpawn = enemyPrefabs[randomIndex];
        }

        if (enemyToSpawn != null)
        {
            Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
            // currentEnemyCount will be updated in the next Update cycle by FindGameObjectsWithTag
        }
    }

    /// <summary>
    /// Calculates a random spawn position within the spawn radius,
    /// ensuring it's a minimum distance away from the player.
    /// </summary>
    /// <returns>A valid spawn position or Vector3.zero if none found after attempts.</returns>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPos = Vector3.zero;
        bool validPositionFound = false;
        int attempts = 0;
        int maxAttempts = 10;

        while (!validPositionFound && attempts < maxAttempts)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            randomPos = transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);

            if (playerTransform != null && Vector2.Distance(randomPos, playerTransform.position) >= minSpawnDistanceFromPlayer)
            {
                validPositionFound = true;
            }
            attempts++;
        }

        if (!validPositionFound)
        {
            Debug.LogWarning($"EnemySpawner: Could not find a valid spawn position after {maxAttempts} attempts.");
            return Vector3.zero; // Indicate failure
        }
        return randomPos;
    }

    // Optional: Draw spawn radius in editor for visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        Gizmos.color = Color.cyan;
        if (playerTransform != null)
        {
            Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistanceFromPlayer);
        }
    }
}
