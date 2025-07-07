// GameClient/Assets/Scripts/Enemy/EnemySpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Manages the procedural spawning of enemies. Now tracks active enemies and can
/// be paused for special encounters like the Ethereal Seer.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    public static event Action OnAllEnemiesCleared;

    [Header("Spawner Configuration")]
    [Tooltip("List of regular 'fodder' enemy types this spawner can instantiate.")]
    public List<EnemyData> fodderEnemies;
    [Tooltip("List of 'elite' enemy types this spawner can instantiate.")]
    public List<EnemyData> eliteEnemies;

    [Header("Unique Elites (Limit 1)")]
    [Tooltip("A list of elite types that should only have one instance active at a time.")]
    public List<EnemyData> uniqueEliteData;

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

    // --- Private State ---
    private float spawnTimer;
    private Transform playerTransform;
    private List<EnemyData> runtimeElitePool;
    private bool isSpawningPaused = false;
    private List<GameObject> clearanceEnemies = new List<GameObject>();
    private List<GameObject> exemptEnemies = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

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
            return;
        }

        runtimeElitePool = new List<EnemyData>(eliteEnemies);
        runtimeElitePool.AddRange(uniqueEliteData);
    }

    private void Update()
    {
        if (playerTransform == null || isSpawningPaused) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            if ((clearanceEnemies.Count + exemptEnemies.Count) < maxEnemiesOnScreen)
            {
                SpawnEnemy();
            }
            spawnTimer = spawnInterval;
        }
    }

    private void LateUpdate()
    {
        // Clean up the list by removing any enemies that were destroyed.
        clearanceEnemies.RemoveAll(item => item == null);
        exemptEnemies.RemoveAll(item => item == null);

        // If the game is waiting for the Seer and all enemies are now gone, fire the event.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.AwaitingSeer && clearanceEnemies.Count == 0)
        {
            // Check if spawning is paused to ensure this only fires once after being triggered.
            if (isSpawningPaused)
            {
                OnAllEnemiesCleared?.Invoke();
            }
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

    public void SpawnSpecialEnemy(EnemyData specialEnemyData)
    {
        if (specialEnemyData == null)
        {
            Debug.LogError("SpawnSpecialEnemy called with null EnemyData.", this);
            return;
        }
        InstantiateAndInitializeEnemy(specialEnemyData);
    }

    public void SpawnSpecialEnemyAt(EnemyData specialEnemyData, Vector3 spawnPosition)
    {
        if (specialEnemyData == null)
        {
            Debug.LogError("SpawnSpecialEnemyAt called with null EnemyData.", this);
            return;
        }
        // This reuses your existing instantiation and initialization logic
        InstantiateAndInitializeEnemyAt(specialEnemyData, spawnPosition);
    }

    private void InstantiateAndInitializeEnemyAt(EnemyData enemyData, Vector3 spawnPosition)
    {
        if (enemyData.visualPrefab == null)
        {
            Debug.LogError($"EnemyData '{enemyData.name}' has no visual prefab assigned.", enemyData);
            return;
        }
        if (uniqueEliteData.Contains(enemyData))
        {
            runtimeElitePool.Remove(enemyData);
            Debug.Log($"Spawning unique elite '{enemyData.name}' and removing it from the pool.");
        }

        GameObject enemyInstance = Instantiate(enemyData.visualPrefab, spawnPosition, Quaternion.identity, this.transform);

        // Add the newly spawned enemy to our tracking list.
        if (enemyData.isExemptFromClearanceChecks) { exemptEnemies.Add(enemyInstance); }
        else { clearanceEnemies.Add(enemyInstance); }

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

    private void InstantiateAndInitializeEnemy(EnemyData enemyData)
    {
        InstantiateAndInitializeEnemyAt(enemyData, GetRandomSpawnPosition());
    }

    private EnemyData ChooseEnemyType()
    {
        bool canSpawnElite = runtimeElitePool.Any() &&
                             GameManager.Instance != null &&
                             GameManager.Instance.currentWave >= eliteStartWave &&
                             UnityEngine.Random.value < eliteSpawnChance;

        if (canSpawnElite)
        {
            return runtimeElitePool[UnityEngine.Random.Range(0, runtimeElitePool.Count)];
        }

        if (fodderEnemies.Any())
        {
            return fodderEnemies[UnityEngine.Random.Range(0, fodderEnemies.Count)];
        }
        return null;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle), 0);
        float distance = UnityEngine.Random.Range(minSpawnDistanceFromPlayer, spawnRadius);

        return playerTransform.position + direction * distance;
    }

    public void OnUniqueEnemyDefeated(EnemyData defeatedEnemyData)
    {
        if (uniqueEliteData.Contains(defeatedEnemyData) && !runtimeElitePool.Contains(defeatedEnemyData))
        {
            runtimeElitePool.Add(defeatedEnemyData);
            Debug.Log($"Added '{defeatedEnemyData.name}' back to the elite spawn pool.");
        }
    }

    /// <summary>
    /// Pauses all enemy spawning activities.
    /// </summary>
    public void StopSpawning()
    {
        isSpawningPaused = true;
        Debug.Log("Enemy spawning has been paused.");
    }

    /// <summary>
    /// Resumes all enemy spawning activities.
    /// </summary>
    public void StartSpawning()
    {
        isSpawningPaused = false;
        spawnTimer = spawnInterval; // Reset timer to prevent instant spawn.
        Debug.Log("Enemy spawning has been resumed.");
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = new Color(1, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistanceFromPlayer);
        Gizmos.color = new Color(1, 0, 0, 0.25f);
        Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
    }

    public void DespawnAllExemptEnemies()
    {
        if (exemptEnemies.Count > 0)
        {
            Debug.Log($"Despawning {exemptEnemies.Count} exempt enemies due to a system event.");
            // Iterate backwards through a copy of the list because the Despawn method might modify the original list.
            foreach (var enemy in exemptEnemies.ToList())
            {
                if (enemy != null)
                {
                    // This robustly checks if the enemy is a Vexer with a special Despawn method.
                    if (enemy.TryGetComponent<VectorVexerController>(out var vexer))
                    {
                        vexer.Despawn(VectorVexerController.DespawnReason.ForcedBySystem);
                    }
                    else
                    {
                        // Fallback for other exempt types that might not have a special Despawn method.
                        Destroy(enemy);
                    }
                }
            }
            exemptEnemies.Clear();
        }
    }
}