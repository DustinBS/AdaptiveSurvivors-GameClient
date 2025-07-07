// GameClient/Assets/Scripts/Managers/PlayerProfileInitializer.cs

using UnityEngine;

/// <summary>
/// A persistent singleton responsible for initializing core, persistent player data
/// at the very start of the game. It ensures a Player ID exists before any other
/// service might need it.
/// </summary>
public class PlayerProfileInitializer : MonoBehaviour
{
    public static PlayerProfileInitializer Instance { get; private set; }

    [Header("Data References")]
    [Tooltip("Reference to the main PlayerData asset that holds the player's profile.")]
    [SerializeField] private PlayerData playerData;

    [Header("Testing Overrides")]
    [Tooltip("(Editor Only) If not empty, this ID will be used instead of the auto-generated one.")]
    [SerializeField] private string editorTestPlayerID;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure the PlayerData reference is set in the Inspector
        if (playerData == null)
        {
            Debug.LogError("PlayerProfileInitializer: PlayerData reference is not set! Cannot ensure Player ID.", this);
            enabled = false;
            return;
        }

        // --- Use the override if available in the editor ---
        #if UNITY_EDITOR
        // This block of code will only be included in Unity Editor builds.
        // It will be completely stripped out when you build the final game.
        if (!string.IsNullOrEmpty(editorTestPlayerID))
        {
            playerData.playerID = editorTestPlayerID;
            // Also, save it to PlayerPrefs to ensure consistency if anything checks there.
            PlayerPrefs.SetString("player_id", editorTestPlayerID);
            Debug.LogWarning($"<color=orange>EDITOR OVERRIDE:</color> Using test Player ID: '{editorTestPlayerID}'");
            return; // Skip the normal generation logic
        }
        #endif

        // If we are in a real build or the test ID is empty, run the normal logic.
        playerData.EnsurePlayerID();
    }
}