// GameClient/Assets/Scripts/Enemy/EnemySpawner.cs
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public class Wave
    {
        public string name;
        public List<WaveComponent> enemies;
        public float spawnInterval;
    }

    [Serializable]
    public class WaveComponent
    {
        public EnemyData enemyData;
        public int count;
    }

    [Header("Spawning Configuration")]
    [SerializeField] private List<Wave> waves;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Dependencies")]
    [Tooltip("Unique identifier for this spawner instance.")]
    public string spawnerId = "spawner_01";
    private KafkaClient kafkaClient;

    private int currentWaveIndex = 0;

    void Awake()
    {
        kafkaClient = FindObjectOfType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogWarning("EnemySpawner: KafkaClient not found. Events will not be sent.", this);
        }
    }

    void Start()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned to the EnemySpawner.", this);
            enabled = false;
            return;
        }
        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        while (currentWaveIndex < waves.Count)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
            currentWaveIndex++;
        }
        Debug.Log("All waves completed.");
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        Debug.Log("Spawning Wave: " + wave.name);
        foreach (var component in wave.enemies)
        {
            for (int i = 0; i < component.count; i++)
            {
                SpawnEnemy(component.enemyData);
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }
    }

    private void SpawnEnemy(EnemyData enemyData)
    {
        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject enemyInstance = Instantiate(enemyData.enemyPrefab, spawnPoint.position, Quaternion.identity);

        // Initialize components on the spawned enemy
        var enemyHealth = enemyInstance.GetComponent<EnemyHealth>();
        if (enemyHealth) enemyHealth.Initialize(enemyData);

        var enemyMovement = enemyInstance.GetComponent<EnemyMovement>();
        if (enemyMovement) enemyMovement.Initialize(enemyData);

        var enemyAttack = enemyInstance.GetComponent<EnemyAttack>();
        if (enemyAttack)
        {
            // [FIX] Pass the entire EnemyData object to the Initialize method
            enemyAttack.Initialize(enemyData);
        }

        SendEnemySpawnEvent(enemyInstance.GetInstanceID().ToString(), enemyData.enemyID);
    }

    private void SendEnemySpawnEvent(string instanceId, string enemyType)
    {
        if (kafkaClient == null) return;

        var payload = new Dictionary<string, object>
        {
            { "spawner_id", spawnerId },
            { "enemy_instance_id", instanceId },
            { "enemy_type", enemyType },
            { "wave", GameManager.Instance.currentWave }
        };
        kafkaClient.SendGameplayEvent("enemy_spawn_event", "game_logic", payload);
    }
}
