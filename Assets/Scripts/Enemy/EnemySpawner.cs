// GameClient/Assets/Scripts/Enemy/EnemySpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages spawning enemies. Now updated to work with the EnemyBrain and Strategy pattern.
/// It initializes the EnemyBrain with strategies defined in the EnemyData asset.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    [Tooltip("List of regular 'fodder' enemy types this spawner can instantiate.")]
    public List<EnemyData> fodderEnemies;

    [Tooltip("List of 'elite' enemy types this spawner can instantiate.")]
    public List<EnemyData> eliteEnemies;

    [Header("Spawn Timings & Limits")]
    [Tooltip("The interval (in seconds) between spawn attempts.")]
    public float spawnInterval = 1.0f;

    [Tooltip("The maximum number of enemies allowed on screen at once.")]
    public int maxEnemiesOnScreen = 50;

    [Header("Spawn Area")]
    [Tooltip("The radius around the player where enemies can spawn.")]
    public float spawnRadius = 20f;

    [Tooltip("The minimum distance from the player to spawn enemies, to avoid spawning on top of them.")]
    public float minSpawnDistanceFromPlayer = 15f;

    [Header("Elite Spawning Logic")]
    [Tooltip("The wave number at which elites can start appearing.")]
    public int eliteStartWave = 3;
    [Tooltip("The chance (0 to 1) to spawn an elite instead of a regular enemy, if conditions are met.")]
    [Range(0f, 1f)]
    public float eliteSpawnChance = 0.1f;

    private float spawnTimer;
    private Transform playerTransform; // Cached reference to the player

    void Start()
    {
        // Cache the player's transform at the start of the scene for performance.
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

        if (spawnTimer <= 0f)
        {
            // Simple check to avoid FindObjectsOfType if possible
            if (transform.childCount < maxEnemiesOnScreen)
            {
                SpawnEnemy();
            }
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        EnemyData enemyToSpawnData = ChooseEnemyType();
        if (enemyToSpawnData == null || enemyToSpawnData.visualPrefab == null) return;

        // Ensure that the strategies are assigned in the EnemyData asset
        if (enemyToSpawnData.movementStrategy == null || enemyToSpawnData.attackStrategy == null)
        {
            Debug.LogError($"EnemyData '{enemyToSpawnData.name}' is missing a movement or attack strategy. Cannot spawn.", enemyToSpawnData);
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();
        if (spawnPosition == Vector3.zero) return;

        GameObject enemyInstance = Instantiate(enemyToSpawnData.visualPrefab, spawnPosition, Quaternion.identity, this.transform);

        // Initialize Health
        EnemyHealth enemyHealth = enemyInstance.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.Initialize(enemyToSpawnData);
        }

        // Get the EnemyBrain component
        EnemyBrain brain = enemyInstance.GetComponent<EnemyBrain>();
        if (brain != null)
        {
            // Initialize the brain with the strategies and base stats from the data asset.
            // The base stats are now correctly sourced from the strategy assets themselves.
            brain.Initialize(
                playerTransform,
                enemyToSpawnData.movementStrategy,
                enemyToSpawnData.attackStrategy,
                enemyToSpawnData.movementStrategy.baseSpeed,
                enemyToSpawnData.attackStrategy.baseDamage
            ); //
        }
        else
        {
            Debug.LogError($"Spawned enemy '{enemyToSpawnData.name}' is missing an EnemyBrain component on its prefab.", enemyInstance);
        }
    }

    private EnemyData ChooseEnemyType()
    {
        bool canSpawnElite = eliteEnemies.Any() &&
                             GameManager.Instance != null &&
                             GameManager.Instance.currentWave >= eliteStartWave &&
                             Random.value < eliteSpawnChance;

        if (canSpawnElite)
        {
            return eliteEnemies[Random.Range(0, eliteEnemies.Count)];
        }

        if (fodderEnemies.Any())
        {
            return fodderEnemies[Random.Range(0, fodderEnemies.Count)];
        }

        return null;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (playerTransform == null) return Vector3.zero;

        for (int i = 0; i < 10; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = playerTransform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * spawnRadius;

            if (Vector3.Distance(spawnPos, playerTransform.position) >= minSpawnDistanceFromPlayer)
            {
                return spawnPos;
            }
        }
        return playerTransform.position + (Vector3.right * spawnRadius);
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = new Color(1, 0, 0, 0.25f);
        Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
        Gizmos.color = new Color(1, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistanceFromPlayer);
    }
}
