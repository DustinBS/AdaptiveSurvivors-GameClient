// GameClient/Assets/Scripts/Enemy/AI/BossBrain.cs

using UnityEngine;

/// <summary>
/// A specialized brain for boss enemies, inheriting from EnemyBrain.
/// This class can be extended to include complex boss-specific mechanics like
/// phase changes, special attacks, or spawning reinforcements.
/// </summary>
public class BossBrain : EnemyBrain
{
    [Header("Boss Mechanics")]
    [Tooltip("The type of minion this boss spawns.")]
    [SerializeField] private EnemyData minionToSpawn;

    [Tooltip("The time, in seconds, between each minion spawn.")]
    [SerializeField] private float spawnInterval = 10f;

    [Tooltip("How far away from the boss minions should spawn.")]
    [SerializeField] private float spawnRadius = 5f;

    [Tooltip("How many minions to spawn in a single burst.")]
    [SerializeField] private int minionsPerBurst = 2;

    private float spawnTimer;

    protected override void Update()
    {
        // Call the base Update logic from EnemyBrain (for attack strategy)
        base.Update();

        // If we don't have a minion to spawn, don't run the timer logic.
        if (minionToSpawn == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnMinion();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnMinion()
    {
        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("BossBrain: Cannot spawn minion because EnemySpawner.Instance is not found.", this);
            return;
        }

        Debug.Log($"{Data.enemyName} is spawning a burst of {minionsPerBurst} reinforcements!");

        for (int i = 0; i < minionsPerBurst; i++)
        {
            // Calculate a random position in a circle around the boss
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle), 0);
            Vector3 spawnPosition = transform.position + direction * spawnRadius;

            // Use the new spawner method to spawn at our calculated position
            EnemySpawner.Instance.SpawnSpecialEnemyAt(minionToSpawn, spawnPosition);
        }
    }
}