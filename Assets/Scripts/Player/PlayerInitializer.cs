// GameClient/Assets/Scripts/Player/PlayerInitializer.cs
using UnityEngine;

/// <summary>
/// This script acts as the bridge between the persistent PlayerData ScriptableObject
/// and the player's in-scene components. It runs once on Awake to configure the
/// player's stats and components based on the character selected in a previous scene.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerExperience))]
public class PlayerInitializer : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private DefaultCharacterAttributes defaultAttributes;

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

        // Get the final character data and player ID we will be using for initialization.
        CharacterData characterToLoad = playerData.characterData;
        string authoritativePlayerId = playerData.playerID;


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

        // --- Core Component Initialization ---
        var playerStats = GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = gameObject.AddComponent<PlayerStats>();

        // 1. Initialize PlayerStats. This clears any old data.
        playerStats.Initialize(authoritativePlayerId);

        // 2. Apply the UNIVERSAL DEFAULT stats first.
        if (defaultAttributes != null) playerStats.ApplyBaseAttributes(defaultAttributes.defaultAttributes);

        // 3. Apply the CHARACTER-SPECIFIC stats. These will override any defaults.
        playerStats.ApplyBaseAttributes(characterToLoad.baseAttributes);

        // 4. Apply the STARTING WEAPON'S base stats.
        playerStats.ApplyWeaponStats(characterToLoad.startingWeapon);

        // 5. Finalize initialization (e.g. for health UI).
        playerStats.FinalizeInitialization();

        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.Initialize(characterToLoad);

        Debug.Log($"Player initialized successfully with Player ID: {authoritativePlayerId} and Character: {characterToLoad.characterName}");
    }
}