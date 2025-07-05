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
    public string playerId { get; private set; }
    public float maxHealth { get; private set; }
    public float currentHealth { get; private set; }

    [Tooltip("Reduces incoming damage by a flat percentage. 0.1 = 10% reduction.")]
    public float armor = 0f;

    public float maxMana = 50f;
    public float currentMana;
    public List<string> activeBuffs = new List<string>();
    public List<string> activeDebuffs = new List<string>();

    [Header("Kafka Event Settings")]
    [SerializeField] private float statusEventSendInterval = 1.0f;

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

    // The Initialize method, called by PlayerInitializer
    public void Initialize(CharacterData data)
    {
        this.playerId = data.characterName;
        this.maxHealth = data.baseHealth;
        this.currentHealth = this.maxHealth; // Start with full health
    }

    void Awake()
    {
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

    // --- Public API for Health & Damage ---

    /// <summary>
    /// Modifies the player's maximum health by a flat amount or percentage.
    /// Also heals the player by the amount of max health gained.
    /// </summary>
    public void ModifyMaxHealth(float amount, bool isPercentage)
    {
        if (isDead) return;

        float changeAmount;
        if (isPercentage)
        {
            changeAmount = maxHealth * amount;
        }
        else
        {
            changeAmount = amount;
        }

        maxHealth += changeAmount;
        if (changeAmount > 0)
        {
            Heal(changeAmount); // Heal for positive changes.
        }
        else
        {
            // If max health is reduced, ensure current health isn't higher than the new max.
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
    }

    public void ModifyArmor(float amount)
    {
        if (isDead) return;
        armor += amount;
        // Clamp armor between 0% and a max of 90% reduction.
        armor = Mathf.Clamp(armor, 0f, 0.9f);
    }

    /// <summary>
    /// Reduces player health and sends a Kafka event detailing the damage taken.
    /// </summary>
    /// <param name="amount">The raw amount of damage dealt.</param>
    /// <param name="sourceEnemyId">The ID of the enemy that dealt the damage.</param>
    /// <param name="isSourceElite">A flag indicating if the damage source is an elite.</param>
    public void TakeDamage(float amount, string sourceEnemyId, bool isSourceElite)
    {
        if (isDead) return;

        float finalDamage = amount * (1 - armor);
        if (finalDamage < 0) finalDamage = 0;

        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        SendDamageTakenEvent(finalDamage, sourceEnemyId, isSourceElite);

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
            { "max_hp", maxHealth },
            { "armor", armor },
            { "max_mana", maxMana },
            { "mana", currentMana },
            { "active_buffs", activeBuffs.ToList() },
            { "active_debuffs", activeDebuffs.ToList() }
        };

        kafkaClient.SendGameplayEvent("player_status_event", playerId, payload);
    }

    private void SendDamageTakenEvent(float damageAmount, string enemyId, bool isElite)
    {
        if (kafkaClient == null) return;
        var payload = new Dictionary<string, object>
        {
            { "dmg_amount", damageAmount },
            { "source_enemy_id", enemyId },
            { "is_elite_source", isElite }
        };
        kafkaClient.SendGameplayEvent("player_damage_taken_event", this.playerId, payload);
    }
}
