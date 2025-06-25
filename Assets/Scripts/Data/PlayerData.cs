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
    [Header("Character Choice")]
    [Tooltip("The base character data selected for the current run. This is set by the Character Selection screen.")]
    public CharacterData characterData;

    [Header("Runtime Stats")]
    [Tooltip("The player's current health during a run. This value can change.")]
    public float currentHealth;

    [Tooltip("The player's current damage during a run. This can be modified by upgrades.")]
    public float currentDamage;

    // A dictionary to hold historical (lifetime) statistics.
    [Header("Historical Stats")]
    [Tooltip("Persistent, lifetime statistics for this player profile.")]
    public Dictionary<string, long> historicalStats = new Dictionary<string, long>();

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

        // Copy the base stats from the selected CharacterData into the PlayerData's runtime fields.
        // This establishes the starting conditions for the run.
        currentHealth = characterData.baseHealth;
        currentDamage = characterData.baseDamage;

        // In the future, you would reset other run-specific data here as well.
        // For example:
        // currentExperience = 0;
        // currentLevel = 1;
        // activeUpgrades.Clear();

        Debug.Log($"PlayerData initialized for new run with character: '{characterData.characterName}'. Base Health: {currentHealth}, Base Damage: {currentDamage}");
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

}
