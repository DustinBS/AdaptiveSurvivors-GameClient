// GameClient/Assets/Scripts/Managers/GameManager.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A scene-specific manager that controls the overall game state, including wave progression,
/// game time, and pausing. It listens for critical events like player death to transition game states.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Paused, GameOver }

    // --- Singleton Instance ---
    // Provides easy, static access to the manager from other scripts in the same scene.
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [Tooltip("The current wave number.")]
    public int currentWave = 1;
    [Tooltip("The time elapsed since the start of the current run (in seconds).")]
    public float timeElapsed = 0f;

    // The current state of the game. Making it public allows other scripts to check it if needed.
    public GameState CurrentState { get; private set; }

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
    }

    void OnDisable()
    {
        // ALWAYS unsubscribe from static events on disable/destroy to prevent memory leaks.
        PlayerStatus.OnPlayerDeath -= HandlePlayerDeath;
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
        if (enemySpawner != null && waveEndElite != null)
        {
            enemySpawner.SpawnSpecialEnemy(waveEndElite);
        }
        else
        {
            Debug.LogWarning("Cannot spawn wave end elite. Spawner or elite data is not assigned in GameManager.", this);
        }

        currentWave++;
        Debug.Log($"Advancing to Wave {currentWave}!");
    }
}
