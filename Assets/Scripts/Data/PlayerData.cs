// GameClient/Assets/Scripts/Data/PlayerData.cs

using UnityEngine;

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

    // Note: The original playerName and playerPortrait fields have been removed.
    // That information should now be accessed through the 'characterData' reference
    // to keep a single source of truth (e.g., PlayerData.characterData.characterName).

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
}
