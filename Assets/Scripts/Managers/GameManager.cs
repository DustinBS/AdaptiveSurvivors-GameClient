// GameClient/Assets/Scripts/Managers/GameManager.cs

using UnityEngine;
using System.Collections.Generic;

// This script manages the overall game state, including tracking waves, elapsed time,
// and area explored. It periodically sends 'game_state_event' to Kafka.
// It also serves as a central point for managing game flow.

public class GameManager : MonoBehaviour
{
    [Header("Game State Settings")]
    [Tooltip("The unique ID of the player currently playing.")]
    public string playerId = "player_001"; // Should match other player scripts

    [Tooltip("The current wave number.")]
    public int currentWave = 1;

    [Tooltip("The time elapsed since the start of the current run (in seconds).")]
    public float timeElapsed = 0f;

    [Tooltip("An estimated percentage of the game area explored (0-100).")]
    [Range(0f, 100f)]
    public float areaExploredPercentage = 0f;

    [Header("Kafka Event Settings")]
    [Tooltip("How often (in seconds) the game_state_event is sent to Kafka.")]
    public float gameStateEventSendInterval = 5.0f; // Send state every 5 seconds

    private KafkaClient kafkaClient;
    private float gameStateEventTimer;

    // --- Singleton Pattern (Optional but often useful for GameManagers) ---
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep GameManager alive across scenes if needed

        kafkaClient = FindObjectOfType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("GameManager: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
        }

        gameStateEventTimer = gameStateEventSendInterval;
    }

    void Update()
    {
        timeElapsed += Time.deltaTime; // Increment elapsed time

        // Update areaExploredPercentage (placeholder logic)
        // In a real game, this would be calculated based on player movement, revealed map, etc.
        if (areaExploredPercentage < 100f)
        {
            areaExploredPercentage = Mathf.Min(100f, timeElapsed / 60f * 10f); // Example: gain 10% per minute, capping at 100%
        }

        // Periodically send game state to Kafka
        gameStateEventTimer -= Time.deltaTime;
        if (gameStateEventTimer <= 0)
        {
            SendGameStateEvent();
            gameStateEventTimer = gameStateEventSendInterval;
        }

        // Example: Advance wave every 60 seconds
        if (timeElapsed >= currentWave * 60f)
        {
            AdvanceWave();
        }
    }

    /// <summary>
    /// Advances the game to the next wave.
    /// </summary>
    public void AdvanceWave()
    {
        currentWave++;
        Debug.Log($"Advancing to Wave {currentWave}!");
        // Trigger enemy spawners, update UI, etc.
    }

    /// <summary>
    /// Ends the current game run.
    /// This could be called on player death, win condition, or explicit exit.
    /// </summary>
    public void EndRun()
    {
        Debug.Log("Game Run Ended!");
        // Perform end-of-run actions:
        // - Save persistent data (e.g., gold for meta-progression)
        // - Load post-run hub scene
        // - Trigger LLM NPC commentary request (if applicable)
        // For POC, simply log and stop updates.
        enabled = false; // Stop updating this script

        // Example: Trigger LLM NPC commentary request
        // This is a placeholder; actual implementation will be more complex.
        // Needs player's run summary data to be available here.
        StartCoroutine(RequestNpcCommentary("sarcastic_merchant"));
    }

    /// <summary>
    /// Sends the current game state (wave, time elapsed, area explored) to Kafka.
    /// </summary>
    private void SendGameStateEvent()
    {
        var payload = new Dictionary<string, object>
        {
            { "wave", currentWave },
            { "time_elapsed", timeElapsed },
            { "area_explored_percent", areaExploredPercentage }
        };

        kafkaClient.SendGameplayEvent("game_state_event", playerId, payload);
        // Debug.Log("Game state event sent to Kafka.");
    }

    /// <summary>
    /// Makes an HTTP request to the Cloud Function for NPC commentary.
    /// This is a simple placeholder and will need proper async handling,
    /// error checking, and UI integration.
    /// </summary>
    /// <param name="npcPersonality">The personality string for the NPC.</param>
    private System.Collections.IEnumerator RequestNpcCommentary(string npcPersonality)
    {
        string cloudFunctionUrl = "YOUR_CLOUD_FUNCTION_HTTP_TRIGGER_URL"; // IMPORTANT: Replace with your actual Cloud Function URL

        var requestData = new Dictionary<string, string>
        {
            { "player_id", playerId },
            { "npc_personality", npcPersonality }
        };
        string jsonPayload = JsonUtility.ToJson(requestData); // Using Unity's built-in JsonUtility

        Debug.Log($"Requesting NPC commentary from: {cloudFunctionUrl} with payload: {jsonPayload}");

        using (UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequest.Post(cloudFunctionUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            webRequest.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error requesting NPC commentary: {webRequest.error}");
            }
            else
            {
                string responseText = webRequest.downloadHandler.text;
                Debug.Log($"NPC Commentary Response: {responseText}");
                // Parse the JSON response and display commentary in UI
                // Example:
                // var responseJson = JsonUtility.FromJson<NpcCommentaryResponse>(responseText);
                // Debug.Log($"Generated Commentary: {responseJson.commentary}");
            }
        }
    }

    // Dummy class for parsing Cloud Function response (adjust based on actual JSON structure)
    // [System.Serializable]
    // private class NpcCommentaryResponse
    // {
    //     public string commentary;
    // }
}
