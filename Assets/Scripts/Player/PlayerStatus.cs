// GameClient/Assets/Scripts/Player/PlayerStatus.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStatus : MonoBehaviour
{
    [Header("Data References")]
    [Tooltip("The PlayerData ScriptableObject holding the player's current and max stats.")]
    public PlayerData playerData; // [FIX] Made public to be accessible by other scripts

    [Header("Dependencies")]
    [Tooltip("Unique identifier for the player.")]
    public string playerId = "player_001";

    public float currentHealth;
    public float maxHealth;

    private KafkaClient kafkaClient;

    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDeath;

    void Awake()
    {
        kafkaClient = FindObjectOfType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogWarning("PlayerStatus: KafkaClient not found. Death events will not be sent.", this);
        }

        if (playerData == null || playerData.characterData == null)
        {
            Debug.LogError("PlayerStatus: PlayerData or its CharacterData is not assigned. Disabling component.", this);
            enabled = false;
            return;
        }

        // Initialize stats from the PlayerData asset at the start.
        maxHealth = playerData.characterData.baseHealth;
        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0); // Prevent health from going below zero

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Prevent health from exceeding max

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        OnPlayerDeath?.Invoke();
        SendPlayerDeathEvent();

        // Disable player components to prevent further actions
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerAttack>().enabled = false;
        gameObject.SetActive(false); // Or handle death animation/screen
    }

    private void SendPlayerDeathEvent()
    {
        if (kafkaClient == null) return;

        var payload = new Dictionary<string, object>
        {
            { "final_level", GetComponent<PlayerExperience>()?.currentLevel ?? 1 },
            { "cause_of_death", "enemy_attack" } // Can be expanded later
        };
        kafkaClient.SendGameplayEvent("player_death_event", playerId, payload);
    }
}
