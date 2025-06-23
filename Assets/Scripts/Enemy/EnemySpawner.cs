// GameClient/Assets/Scripts/Enemy/EnemySpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the procedural spawning of enemies. Calls the centralized Initialize method
/// on the EnemyBrain, decoupling the spawner from enemy-specific setup logic.
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
    [Tooltip("The maximum radius around the player where enemies can spawn.")]
    public float spawnRadius = 20f;
    [Tooltip("The minimum distance from the player to spawn enemies.")]
    public float minSpawnDistanceFromPlayer = 15f;

    [Header("Elite Spawning Logic")]
    [Tooltip("The wave number at which elites can start appearing.")]
    public int eliteStartWave = 3;
    [Tooltip("The chance (0 to 1) to spawn an elite instead of a regular enemy.")]
    [Range(0f, 1f)]
    public float eliteSpawnChance = 0.1f;

    private float spawnTimer;
    private Transform playerTransform;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("EnemySpawner: Player GameObject not found. Spawner disabled.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
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
        if (enemyToSpawnData != null)
        {
            InstantiateAndInitializeEnemy(enemyToSpawnData);
        }
    }

    /// <summary>
    /// Spawns a specific enemy, typically for a boss wave or special event.
    /// </summary>
    /// <param name="specialEnemyData">The EnemyData for the special enemy.</param>
    public void SpawnSpecialEnemy(EnemyData specialEnemyData)
    {
        if (specialEnemyData == null)
        {
            Debug.LogError("SpawnSpecialEnemy called with null EnemyData.", this);
            return;
        }

        InstantiateAndInitializeEnemy(specialEnemyData);
    }

    /// <summary>
    /// A unified, robust method for instantiating and initializing any enemy.
    /// </summary>
    private void InstantiateAndInitializeEnemy(EnemyData enemyData)
    {
        if (enemyData.visualPrefab == null)
        {
            Debug.LogError($"EnemyData '{enemyData.name}' has no visual prefab assigned.", enemyData);
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject enemyInstance = Instantiate(enemyData.visualPrefab, spawnPosition, Quaternion.identity, this.transform);

        // The spawner's only job is to get the brain and kick off initialization.
        if (enemyInstance.TryGetComponent<EnemyBrain>(out var brain))
        {
            brain.Initialize(playerTransform, enemyData);
        }
        else
        {
            Debug.LogError($"Spawned enemy '{enemyData.name}' is missing an EnemyBrain component. Destroying instance.", enemyInstance);
            Destroy(enemyInstance);
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

        // Default to spawning a fodder enemy if available.
        if (fodderEnemies.Any())
        {
            return fodderEnemies[Random.Range(0, fodderEnemies.Count)];
        }

        return null;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Calculate a random angle and distance for a point on a ring.
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle), 0);
        float distance = Random.Range(minSpawnDistanceFromPlayer, spawnRadius);

        return playerTransform.position + direction * distance;
    }

    private void OnDrawGizmosSelected()
    {
        // This visual aid is helpful for debugging spawn distances in the editor.
        if (playerTransform == null) return;
        Gizmos.color = new Color(1, 1, 0, 0.25f); // Yellow for minimum distance
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistanceFromPlayer);
        Gizmos.color = new Color(1, 0, 0, 0.25f); // Red for maximum distance
        Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
    }
}