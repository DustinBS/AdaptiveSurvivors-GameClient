// GameClient/Assets/Scripts/Managers/GameManager.cs

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A scene-specific manager that controls the overall game state, including wave progression,
/// game time, and pausing. It listens for critical events like player death to transition game states.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Paused, GameOver, AwaitingSeer, SeerEncounter }

    // --- Singleton Instance ---
    // Provides easy, static access to the manager from other scripts in the same scene.
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState CurrentState { get; private set; }
    [Tooltip("The current wave number.")]
    public int currentWave = 1;
    [Tooltip("The time elapsed since the start of the current run (in seconds).")]
    public float timeElapsed = 0f;

    [Header("Seer System")]
    [Tooltip("A unique identifier for the current run, sent with Kafka events.")]
    public string runId { get; private set; }
    [Tooltip("The Seer encounter will be triggered before waves that are a multiple of this number.")]
    [SerializeField] private int bossWaveInterval = 3;
    [SerializeField] private GameObject seerPrefab;
    private bool isSeerEncounterQueued = false;

    [Header("Wave Boss Spawning")]
    [Tooltip("The EnemyData for the special elite to spawn between waves.")]
    [SerializeField] private EnemyData waveEndElite;
    [Tooltip("Reference to the scene's EnemySpawner component.")]
    [SerializeField] private EnemySpawner enemySpawner;

    // to update historical stats.
    [Header("Data References")]
    [SerializeField] private PlayerData playerData;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        // Generate a unique ID for this run. Mostly used for Seer event tracking
        runId = System.Guid.NewGuid().ToString();
        Debug.Log($"New run started. Run ID: {runId}");

        CurrentState = GameState.Playing;

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        // Reset all run-specific trackers at the start of a new run.
        StatisticsTracker.Reset();
        RunSummaryService.ClearSummary();
    }

    void OnEnable()
    {
        // Subscribe to the static OnPlayerDeath event when this manager is enabled.
        PlayerStatus.OnPlayerDeath += HandlePlayerDeath;
        EnemySpawner.OnAllEnemiesCleared += HandleAllEnemiesCleared;
    }

    void OnDisable()
    {
        // ALWAYS unsubscribe from static events on disable/destroy to prevent memory leaks.
        PlayerStatus.OnPlayerDeath -= HandlePlayerDeath;
        EnemySpawner.OnAllEnemiesCleared -= HandleAllEnemiesCleared;
    }

    void Update()
    {
        // If the game is not in the 'Playing' state, do not run game logic.
        // This is how we achieve a "pause" without freezing the entire game engine.
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        timeElapsed += Time.deltaTime;

        // Example logic for advancing waves.
        if (timeElapsed >= currentWave * 10f)
        {
            AdvanceWave();
        }
    }

    /// <summary>
    /// Sets the game state to AwaitingSeer, pausing game progression.
    /// </summary>
    public void EnterSeerEncounterState()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.AwaitingSeer;
            Debug.Log("Game state changed to AwaitingSeer.");
        }
        DespawnAllVexers();
    }

    /// <summary>
    /// Sets the game state back to Playing, resuming game progression.
    /// </summary>
    public void ExitSeerEncounterState()
    {
        if (CurrentState == GameState.SeerEncounter)
        {
            // 1. Advance the wave number.
            currentWave++;
            Debug.Log($"Seer encounter complete. Advancing to Wave {currentWave}!");

            // 2. Set the state back to Playing.
            CurrentState = GameState.Playing;
            Debug.Log("Game state changed back to Playing.");

            // 3. Explicitly resume the spawner.
            enemySpawner.StartSpawning();
        }
    }

    /// <summary>
    /// The event handler that is called when the PlayerStatus.OnPlayerDeath event is fired.
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        ProcessEndOfRunStatistics();
        StartCoroutine(ShowDeathMenuAfterDelay(1.5f));
    }

    /// <summary>
    /// Gathers run stats, stores them for cross-scene access, and updates persistent historical data.
    /// </summary>
    private void ProcessEndOfRunStatistics()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerData reference is not set in GameManager. Cannot save historical stats.", this);
            return;
        }

        // 1. Get the summary of the completed run from our tracker.
        Dictionary<string, object> runSummary = StatisticsTracker.GenerateRunSummary();

        // 2. Add any stats that are only tracked by GameManager itself.
        runSummary["time_survived_seconds"] = (int)timeElapsed;

        // 3. Store this summary in the transient service for the hub scene to access.
        RunSummaryService.SetRunSummary(runSummary);
        Debug.Log("Run summary stored in RunSummaryService.");

        // 4. Update the persistent historical stats in PlayerData.
        // This pattern ensures historical data is always in sync with completed runs.
        foreach (var stat in runSummary)
        {
            string historicalKey = $"total_{stat.Key}";
            // Ensure we only try to add numerical values.
            if (stat.Value is int || stat.Value is long || stat.Value is float)
            {
                playerData.IncrementHistoricalStat(historicalKey, System.Convert.ToInt64(stat.Value));
            }
        }
        Debug.Log("Historical stats in PlayerData have been updated.");
    }

    // A small delay makes the transition feel less abrupt.
    private IEnumerator ShowDeathMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UGUIManager.Instance.ShowDeathMenu();
    }

    private void AdvanceWave()
    {
        Debug.Log($"Wave {currentWave} complete! Spawning elite boss...");
        // Check if the next wave is a boss wave to trigger the Seer.
        if ((currentWave + 1) % bossWaveInterval == 0)
        {
            isSeerEncounterQueued = true;
            EnterAwaitingSeerState();
        }
        else
        {
            // Spawn normal end-of-wave elite.
            SpawnWaveEndElite();
        }

        currentWave++;
        Debug.Log($"Advancing to Wave {currentWave}!");
    }

    public void EnterAwaitingSeerState()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.AwaitingSeer;
            Debug.Log("Game state changed to AwaitingSeer. Pausing spawner and waiting for enemies to be cleared.");
            enemySpawner.StopSpawning();
        }
    }

    private void SpawnWaveEndElite()
    {
        Debug.Log("Spawning wave-end elite...");
        if (enemySpawner != null && waveEndElite != null)
        {
            enemySpawner.SpawnSpecialEnemy(waveEndElite);
        }
        else
        {
            Debug.LogWarning("Cannot spawn wave end elite. Spawner or elite data is not assigned in GameManager.", this);
        }
    }

    private void HandleAllEnemiesCleared()
    {
        if (isSeerEncounterQueued)
        {
            CurrentState = GameState.SeerEncounter;
            Debug.Log("Game state changed to SeerEncounter. Spawning Seer and despawning Vexers.");

            DespawnAllVexers();
            SpawnSeer();

            isSeerEncounterQueued = false;
        }
    }

    private void DespawnAllVexers()
    {
        var activeVexers = FindObjectsByType<VectorVexerController>(FindObjectsSortMode.None);
        if (activeVexers.Length > 0)
        {
            Debug.Log($"Instructing {activeVexers.Length} Vector Vexer(s) to despawn for the Seer.", this);
            foreach (var vexer in activeVexers)
            {
                vexer.Despawn(VectorVexerController.DespawnReason.ForcedBySystem);
            }
        }
    }

    private void SpawnSeer()
    {
        if (seerPrefab != null)
        {
            Instantiate(seerPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Seer Prefab is not assigned in GameManager!");
        }
    }
}
