// GameClient/Assets/Scripts/Data/PlayerData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New PlayerData", menuName = "Adaptive Survivors/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Character Archetype")]
    [Tooltip("The selected character archetype that defines base stats and abilities.")]
    public CharacterData characterData;

    [Header("Current Run State")]
    [Tooltip("The player's current health during a run.")]
    public float currentHealth;
    [Tooltip("The player's current level.")]
    public int currentLevel;
    [Tooltip("The experience points the player has towards the next level.")]
    public float currentExperience;
    [Tooltip("The experience points required for the next level up.")]
    public float experienceToNextLevel;

    [Header("Inventory")]
    [Tooltip("The player's currently equipped weapon.")]
    public WeaponData equippedWeapon;
    [Tooltip("List of all upgrades the player has acquired in the current run.")]
    public List<UpgradeData> acquiredUpgrades = new List<UpgradeData>();

    /// <summary>
    /// Resets the player's dynamic stats to their starting values based on the CharacterData.
    /// This should be called at the beginning of every run.
    /// </summary>
    public void InitializeForRun()
    {
        if (characterData == null)
        {
            Debug.LogError("Cannot initialize PlayerData, no CharacterData has been assigned!");
            return;
        }

        currentHealth = characterData.baseHealth;
        currentLevel = 1;
        currentExperience = 0;
        experienceToNextLevel = 100; // Or some other initial value
        acquiredUpgrades.Clear();
    }
}
