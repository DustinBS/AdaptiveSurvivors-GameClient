// GameClient/Assets/Scripts/Managers/GameManager.cs

using UnityEngine;
using System.Collections; // Required for IEnumerator

/// <summary>
/// A scene-specific manager that controls the overall game state, including wave progression,
/// game time, and pausing. It listens for critical events like player death to transition game states.
/// </summary>
public class GameManager : MonoBehaviour
{
    // A public enum to clearly define the possible states of the game session.
    public enum GameState
    {
        Playing,
        Paused,
        GameOver
    }

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

    void Awake()
    {
        // Scene-specific singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // The game always starts in the 'Playing' state.
        CurrentState = GameState.Playing;
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
        if (timeElapsed >= currentWave * 60f)
        {
            AdvanceWave();
        }
    }

    /// <summary>
    /// The event handler that is called when the PlayerStatus.OnPlayerDeath event is fired.
    /// </summary>
    private void HandlePlayerDeath()
    {
        CurrentState = GameState.GameOver;
        StartCoroutine(ShowDeathMenuAfterDelay(1.5f));
    }

    // A small delay makes the transition feel less abrupt.
    private IEnumerator ShowDeathMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UIManager.Instance.ShowDeathMenu();
    }

    private void AdvanceWave()
    {
        currentWave++;
        Debug.Log($"Advancing to Wave {currentWave}!");
        // Here you might trigger events for the EnemySpawner to change its behavior.
    }
}
