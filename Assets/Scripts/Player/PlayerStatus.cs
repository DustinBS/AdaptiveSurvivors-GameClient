// GameClient/Assets/Scripts/Player/PlayerStatus.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Manages the player's health and other core stats.
/// Broadcasts events for health changes and player death.
/// </summary>
public class PlayerStatus : MonoBehaviour
{
    [Header("Player Stats")]
    public string playerId = "player_001";
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxMana = 50f;
    public float currentMana;
    public List<string> activeBuffs = new List<string>();
    public List<string> activeDebuffs = new List<string>();

    [Header("Kafka Event Settings")]
    public float statusEventSendInterval = 1.0f;

    // --- Events for other systems to subscribe to ---
    /// <summary>
    /// Event fired when health changes. Parameters: currentHealth (float), maxHealth (float).
    /// </summary>
    public event Action<float, float> OnHealthChanged;
    
    /// <summary>
    /// A static event fired globally when the player's health reaches zero.
    /// Static events can be subscribed to by any script without needing a direct reference to this component instance.
    /// </summary>
    public static event Action OnPlayerDeath;


    private KafkaClient kafkaClient;
    private float statusEventTimer;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;

        kafkaClient = FindAnyObjectByType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("PlayerStatus: KafkaClient not found.", this);
            enabled = false;
        }

        statusEventTimer = statusEventSendInterval;
    }

    void Start()
    {
        // Fire the event on start to initialize UI elements like the health bar.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        statusEventTimer -= Time.deltaTime;
        if (statusEventTimer <= 0)
        {
            SendPlayerStatusEvent();
            statusEventTimer = statusEventSendInterval;
        }
    }

    public void TakeDamage(float amount, string sourceEnemyId = "unknown_enemy")
    {
        // Prevent taking damage if already dead.
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// Handles the player's death sequence.
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died. Broadcasting OnPlayerDeath event.");

        // --- MODIFIED: Invoke the static event ---
        // Any system interested in player death can listen for this.
        OnPlayerDeath?.Invoke();

        // Deactivate the player object to stop movement, attacks, etc.
        gameObject.SetActive(false);
    }

    #region Unchanged Methods
    public bool ConsumeMana(float amount)
    {
        if (isDead) return false;
        if (currentMana >= amount)
        {
            currentMana -= amount;
            return true;
        }
        return false;
    }

    public void AddMana(float amount)
    {
        if (isDead) return;
        currentMana += amount;
        if (currentMana > maxMana) currentMana = maxMana;
    }

    private void SendPlayerStatusEvent()
    {
        var payload = new Dictionary<string, object>
        {
            { "hp", currentHealth },
            { "mana", currentMana },
            { "active_buffs", activeBuffs.ToList() },
            { "active_debuffs", activeDebuffs.ToList() }
        };

        kafkaClient.SendGameplayEvent("player_status_event", playerId, payload);
    }
    #endregion
}
