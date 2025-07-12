// GameClient/Assets/Scripts/Data/PlayerData.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that holds the player's dynamic data for a single game run.
/// It is initialized based on a selected CharacterData, which defines the starting state.
/// This object persists between scenes and acts as the central source of truth for player stats.
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Adaptive Survivors/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Player Identity")]
    [Tooltip("The persistent, unique identifier for this player profile.")]
    [ReadOnly] public string playerID; // Use a custom ReadOnly attribute for safety in Inspector

    [Header("Character Choice")]
    [Tooltip("The base character data selected for the current run. This is set by the Character Selection screen.")]
    public CharacterData characterData;

    // A dictionary to hold historical (lifetime) statistics.
    [Header("Historical Stats")]
    [Tooltip("Persistent, lifetime statistics for this player profile.")]
    public Dictionary<string, long> historicalStats = new Dictionary<string, long>();

    private const string PLAYER_ID_PREFS_KEY = "player_id";

    /// <summary>
    /// Ensures a persistent player ID exists. If not, creates one and saves it.
    /// This should be called once when the game starts.
    /// </summary>
    public void EnsurePlayerID()
    {
        if (!string.IsNullOrEmpty(playerID)) return;

        // Try to load the ID from PlayerPrefs first.
        string savedID = PlayerPrefs.GetString(PLAYER_ID_PREFS_KEY, null);

        if (!string.IsNullOrEmpty(savedID))
        {
            playerID = savedID;
            Debug.Log($"Loaded existing Player ID: {playerID}");
        }
        else
        {
            // If no ID exists, generate a new one and save it.
            playerID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PLAYER_ID_PREFS_KEY, playerID);
            PlayerPrefs.Save();
            Debug.Log($"Generated and saved new Player ID: {playerID}");
        }
    }

    /// <summary>
    /// Initializes the player's stats for the start of a new run based on the selected character.
    /// This should be called from the CharacterSelectController before loading the main game scene.
    /// </summary>
    public void InitializeForRun()
    {
        if (characterData == null)
        {
            Debug.LogError("InitializeForRun called, but no CharacterData has been assigned to PlayerData! Did you select a character?");
            return;
        }

        // In the future, you would reset other run-specific data here as well.
        // For example:
        // currentExperience = 0;
        // currentLevel = 1;
        // activeUpgrades.Clear();

        Debug.Log($"PlayerData initialized for new run with character: '{characterData.characterName}'.");
    }

    /// <summary>
    /// Increments a historical statistic by a given amount.
    /// This should be called by GameManager at the end of a run.
    /// </summary>
    /// <param name="statKey">The key of the stat (e.g., "total_enemies_killed").</param>
    /// <param name="amount">The amount to add.</param>
    public void IncrementHistoricalStat(string statKey, long amount)
    {
        if (!historicalStats.ContainsKey(statKey))
        {
            historicalStats[statKey] = 0;
        }
        historicalStats[statKey] += amount;
    }

    [ContextMenu("Clear Saved Player ID from PlayerPrefs")]
    public void ClearPlayerIDFromPrefs()
    {
        if (PlayerPrefs.HasKey(PLAYER_ID_PREFS_KEY))
        {
            PlayerPrefs.DeleteKey(PLAYER_ID_PREFS_KEY);
            PlayerPrefs.Save();
            Debug.Log($"[Editor Tool] Cleared saved Player ID ('{PLAYER_ID_PREFS_KEY}') from PlayerPrefs.");
        }
        else
        {
            Debug.Log($"[Editor Tool] No Player ID found in PlayerPrefs to clear.");
        }
    }
}