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
    public enum GameState { Playing, Paused, GameOver, AwaitingSeer, SeerEncounter, BossEncounter }

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
    private int seerEncounterCounter = 0;

    [Header("Wave Boss Spawning")]
    [Tooltip("The EnemyData for the special elite to spawn between waves.")]
    [SerializeField] private EnemyData waveEndElite;
    [Tooltip("Reference to the scene's EnemySpawner component.")]
    [SerializeField] private EnemySpawner enemySpawner;
    [Tooltip("The BossData for the next boss encounter.")]
    [SerializeField] private BossData nextBossToSpawn;

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
        PlayerStats.OnPlayerDeath += HandlePlayerDeath;
        EnemySpawner.OnAllEnemiesCleared += HandleAllEnemiesCleared;
        EnemyHealth.OnEnemyDefeated += HandleEnemyDefeated;
    }

    void OnDisable()
    {
        // ALWAYS unsubscribe from static events on disable/destroy to prevent memory leaks.
        PlayerStats.OnPlayerDeath -= HandlePlayerDeath;
        EnemySpawner.OnAllEnemiesCleared -= HandleAllEnemiesCleared;
        EnemyHealth.OnEnemyDefeated -= HandleEnemyDefeated;
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

    public void ExitSeerEncounterState()
    {
        if (CurrentState == GameState.SeerEncounter)
        {
            // Instead of returning to 'Playing', we now trigger the boss fight.
            StartBossEncounter();
        }
    }

    /// <summary>
    /// The event handler that is called when the PlayerStatus.OnPlayerDeath event is fired.
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (CurrentState == GameState.GameOver) return;

        // Check if the player died during the boss fight to record the loss.
        if (CurrentState == GameState.BossEncounter)
        {
            SendBossFightCompletedEvent(false); // `false` for loss
        }

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
        SpawnWaveEndElite();
        if ((currentWave + 1) % bossWaveInterval == 0)
        {
            isSeerEncounterQueued = true;
            EnterAwaitingSeerState();
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

            enemySpawner.DespawnAllExemptEnemies();
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
            // Instantiate the Seer and keep a reference to its GameObject
            GameObject seerObject = Instantiate(seerPrefab, Vector3.zero, Quaternion.identity);

            // Get the controller component and initialize it with the current encounter ID
            if (seerObject.TryGetComponent<SeerController>(out var seerController))
            {
                seerController.Initialize(seerEncounterCounter.ToString());
                seerEncounterCounter++; // Increment the counter for the next Seer
            }
            else
            {
                Debug.LogError("Spawned Seer Prefab is missing a SeerController component!");
            }

            if (KafkaClient.Instance != null && nextBossToSpawn != null)
            {
                var payload = new Dictionary<string, object>
                {
                    // The Flink job needs these to trigger the correct pipeline
                    { "encounter_id", (seerEncounterCounter - 1).ToString() },
                    { "boss_archetype", nextBossToSpawn.archetype.ToString().ToLower() }
                };

                KafkaClient.Instance.SendGameplayEvent("seer_encounter_begin", playerData.playerID, payload);
                Debug.Log($"Sent seer_encounter_begin event for encounter {seerEncounterCounter - 1}");
            }

        }
        else
        {
            Debug.LogError("Seer Prefab is not assigned in GameManager!");
        }
    }


    private void StartBossEncounter()
    {
        CurrentState = GameState.BossEncounter;
        Debug.Log($"Game state changed to BossEncounter. Spawning boss: {nextBossToSpawn.enemyName}");

        if (enemySpawner != null && nextBossToSpawn != null)
        {
            enemySpawner.StopSpawning(); // Ensure no fodder enemies are spawning.
            enemySpawner.SpawnSpecialEnemy(nextBossToSpawn);
        }
        else
        {
            Debug.LogError("Cannot start boss encounter. Spawner or BossData is not assigned in GameManager.", this);
        }
    }

    private void HandleEnemyDefeated(EnemyData defeatedEnemyData)
    {
        // Check if the defeated enemy was a boss and if we are in the boss encounter state.
        if (CurrentState == GameState.BossEncounter && defeatedEnemyData is BossData)
        {
            SendBossFightCompletedEvent(true); // `true` for win

            Debug.Log($"Boss {defeatedEnemyData.enemyName} defeated! Resuming game.");
            CurrentState = GameState.Playing;
            enemySpawner.StartSpawning(); // Resume normal wave spawning.
        }
    }

    private void SendBossFightCompletedEvent(bool didPlayerWin)
    {
        if (KafkaClient.Instance == null)
        {
            Debug.LogWarning("KafkaClient instance not found, cannot send boss_fight_completed event.");
            return;
        }

        var payload = new Dictionary<string, object>
        {
            { "boss_archetype", nextBossToSpawn.archetype.ToString().ToLower() },
            { "win", didPlayerWin } // true: win, false: lose
        };

        if (playerData == null || string.IsNullOrEmpty(playerData.playerID))
        {
            Debug.LogError("PlayerData or PlayerID is not set. Cannot send boss_fight_completed event.", this);
            return;
        }
        string playerId = playerData.playerID;

        KafkaClient.Instance.SendGameplayEvent("boss_fight_completed", playerId, payload);
        Debug.Log($"Sent boss_fight_completed event. Outcome: {(didPlayerWin ? "win" : "loss")}");
    }
}