// GameClient/Assets/Scripts/Player/PlayerInitializer.cs
using UnityEngine;

/// <summary>
/// This script acts as the bridge between the persistent PlayerData ScriptableObject
/// and the player's in-scene components. It runs once on Awake to configure the
/// player's stats based on the character selected in a previous scene.
/// </summary>
[RequireComponent(typeof(PlayerStatus))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerExperience))]
public class PlayerInitializer : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Reference to the PlayerData ScriptableObject that holds the current run's configuration.")]
    [SerializeField] private PlayerData playerData;

    [Header("Fallback Data (for testing)")]
    [Tooltip("The default character to use if none is selected (e.g., when starting the scene directly).")]
    [SerializeField] private CharacterData defaultCharacterData;

    [Tooltip("The default weapon to assign if a character doesn't have one specified.")]
    [SerializeField] private WeaponData defaultWeaponData;
    void Awake()
    {
        // --- Fallback Logic ---
        // If no character data is assigned (e.g., we skipped the character select screen),
        // assign the default character data to the player data for this session.
        if (playerData.characterData == null)
        {
            if (defaultCharacterData != null)
            {
                playerData.characterData = defaultCharacterData;
                Debug.LogWarning($"No character selected in PlayerData. Falling back to default: '{defaultCharacterData.name}'.");
            }
            else
            {
                Debug.LogError("CRITICAL: PlayerData is not set AND no default character is provided. Cannot initialize player.", this);
                gameObject.SetActive(false);
                return;
            }
        }

        // Get the character data we will be using for initialization.
        CharacterData characterToLoad = playerData.characterData;

        // If the chosen character is missing a starting weapon, assign the default one.
        if (characterToLoad.startingWeapon == null)
        {
            if (defaultWeaponData != null)
            {
                characterToLoad.startingWeapon = defaultWeaponData;
                Debug.LogWarning($"Character '{characterToLoad.name}' has no weapon. Assigning default: '{defaultWeaponData.name}'.");
            }
            else
            {
                Debug.LogError($"CRITICAL: Character '{characterToLoad.name}' has no weapon AND no default weapon is provided. Cannot initialize player attack.", this);
            }
        }

        // --- Initialization ---
        // Get references to all the player's core components.
        var status = GetComponent<PlayerStatus>();
        var movement = GetComponent<PlayerMovement>();
        var attack = GetComponent<PlayerAttack>();
        var experience = GetComponent<PlayerExperience>();
        string authoritativePlayerId = playerData.playerID;

        // Call the Initialize methods on each component with the final character data.
        status.Initialize(characterToLoad, authoritativePlayerId);
        movement.Initialize(characterToLoad, authoritativePlayerId);
        attack.Initialize(characterToLoad, authoritativePlayerId);
        experience.Initialize(characterToLoad, authoritativePlayerId);

        Debug.Log($"Player initialized successfully with Player ID: {authoritativePlayerId} and Character: {characterToLoad.characterName}");
    }
}