// GameClient/Assets/Scripts/Player/PlayerStats.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// A centralized component for managing all player attributes and runtime stats.
/// It calculates final stat values from base values and lists of modifiers.
/// This component now handles adding/removing modifiers, managing health AND mana,
/// and sending all relevant Kafka events, making it a complete replacement for PlayerStatus.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("A reference to the AttributeRegistry asset. Used to access specific attribute data.")]
    [SerializeField] private AttributeRegistry attributeRegistry;

    [Header("Kafka Event Settings")]
    [SerializeField] private float statusEventSendInterval = 1.0f;

    // --- Public State ---
    public string playerID { get; private set; }
    public float currentHealth { get; private set; }
    public float currentMana { get; private set; }

    // --- Private State ---
    private readonly Dictionary<AttributeData, float> _baseValues = new Dictionary<AttributeData, float>();
    private readonly Dictionary<AttributeData, List<AttributeModifier>> _modifiers = new Dictionary<AttributeData, List<AttributeModifier>>();
    private KafkaClient kafkaClient;
    private float statusEventTimer;
    private bool isDead = false;

    // --- Events ---
    public event Action<float, float> OnHealthChanged;
    // You can add a similar event for Mana if you create a Mana UI bar.
    // public event Action<float, float> OnManaChanged;
    public static event Action OnPlayerDeath;

    void Awake()
    {
        kafkaClient = FindAnyObjectByType<KafkaClient>();
    }

    void Update()
    {
        // Handle periodic status event sending.
        statusEventTimer -= Time.deltaTime;
        if (statusEventTimer <= 0)
        {
            SendPlayerStatusEvent();
            statusEventTimer = statusEventSendInterval;
        }

        // --- Health Regeneration Logic ---
        if (!isDead)
        {
            // Get the regeneration rate from our attributes.
            float regenPercent = GetAttributeValue(attributeRegistry.HealthRegen);

            if (regenPercent > 0)
            {
                // Calculate healing based on a percentage of max health per second.
                float maxHealth = GetAttributeValue(attributeRegistry.MaxHealth);
                float healingThisFrame = maxHealth * regenPercent * Time.deltaTime;
                Heal(healingThisFrame);
            }
        }
    }

    public void Initialize(string newPlayerId)
    {
        this.playerID = newPlayerId;
        _baseValues.Clear();
        _modifiers.Clear();
    }

    // Create a new public method to apply a list of base attributes
    public void ApplyBaseAttributes(List<CharacterData.BaseAttribute> attributesToApply)
    {
        foreach (var attr in attributesToApply)
        {
            if (attr.attribute == null) continue;
            // This will add a new stat or overwrite an existing one (e.g. default health).
            _baseValues[attr.attribute] = attr.value;
        }
    }

    // Create a method to be called after all stats are applied
    public void FinalizeInitialization()
    {
        // This is where we set currentHealth and invoke the first UI update.
        currentHealth = GetAttributeValue(attributeRegistry.MaxHealth);
        currentMana = GetAttributeValue(attributeRegistry.MaxMana);
        OnHealthChanged?.Invoke(currentHealth, GetAttributeValue(attributeRegistry.MaxHealth));
        statusEventTimer = statusEventSendInterval;
    }

    // --- Attribute Management (Add, Remove, Get) ---
    public float GetAttributeValue(AttributeData attribute)
    {
        _baseValues.TryGetValue(attribute, out float finalValue);
        if (_modifiers.TryGetValue(attribute, out var modifierList))
        {
            foreach (var mod in modifierList.Where(m => !m.IsPercentage)) finalValue += mod.Value;
            foreach (var mod in modifierList.Where(m => m.IsPercentage)) finalValue *= (1 + mod.Value);
        }
        return finalValue;
    }

    /// <summary>
    /// Applies a permanent modification directly to a stat's base value.
    /// Used for permanent upgrades like those from leveling up.
    /// </summary>
    public void ApplyPermanentModifier(AttributeData attribute, float value, bool isPercentage)
    {
        if (!_baseValues.ContainsKey(attribute))
        {
            _baseValues[attribute] = 0;
        }

        if (isPercentage)
        {
            _baseValues[attribute] *= (1 + value);
        }
        else
        {
            _baseValues[attribute] += value;
        }
    }

    /// <summary>
    /// Applies the base stats from a weapon to the character's attributes.
    /// This will overwrite any existing base values for weapon-specific stats.
    /// </summary>
    public void ApplyWeaponStats(WeaponData weapon)
    {
        if (weapon == null) return;

        // Set the base values for stats that come directly from the weapon.
        _baseValues[attributeRegistry.BaseDamage] = weapon.baseDamage;
        _baseValues[attributeRegistry.AttackSpeed] = weapon.attackInterval;
        _baseValues[attributeRegistry.AttackRange] = weapon.attackRange;

        // In the future, a weapon could grant other stats too, like +1 projectile count.
        // _baseValues[attributeRegistry.ProjectileCount] = weapon.projectileCount;
    }

    public void AddModifier(AttributeData attribute, AttributeModifier modifier)
    {
        if (!_modifiers.ContainsKey(attribute)) _modifiers[attribute] = new List<AttributeModifier>();
        _modifiers[attribute].Add(modifier);
    }

    public void RemoveModifier(AttributeData attribute, object source)
    {
        if (_modifiers.TryGetValue(attribute, out var modifierList))
            modifierList.RemoveAll(mod => mod.Source == source);
    }

    // --- Health & Damage Logic ---
    public void TakeDamage(float amount, string sourceEnemyId, bool isSourceElite)
    {
        if (isDead) return;

        float armor = GetAttributeValue(attributeRegistry.Armor);
        float damageReduction = Mathf.Clamp(armor / (armor + 100), 0, 0.9f);
        float finalDamage = amount * (1 - damageReduction);

        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        SendDamageTakenEvent(finalDamage, sourceEnemyId, isSourceElite);
        OnHealthChanged?.Invoke(currentHealth, GetAttributeValue(attributeRegistry.MaxHealth));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        float maxHealth = GetAttributeValue(attributeRegistry.MaxHealth);
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player has died. Broadcasting OnPlayerDeath event.");
        OnPlayerDeath?.Invoke();
        gameObject.SetActive(false);
    }

    // --- Mana Logic ---
    public bool ConsumeMana(float amount)
    {
        if (isDead || currentMana < amount) return false;
        currentMana -= amount;
        // OnManaChanged?.Invoke(currentMana, GetAttributeValue(attributeRegistry.MaxMana));
        return true;
    }

    public void AddMana(float amount)
    {
        if (isDead) return;
        float maxMana = GetAttributeValue(attributeRegistry.MaxMana);
        currentMana += amount;
        if (currentMana > maxMana) currentMana = maxMana;
        // OnManaChanged?.Invoke(currentMana, GetAttributeValue(attributeRegistry.MaxMana));
    }

    // --- Kafka Event Senders ---
    private void SendPlayerStatusEvent()
    {
        if (kafkaClient == null || isDead) return;
        var payload = new Dictionary<string, object>
        {
            { "hp", currentHealth },
            { "max_hp", GetAttributeValue(attributeRegistry.MaxHealth) },
            { "armor", GetAttributeValue(attributeRegistry.Armor) },
            { "mana", currentMana },
            { "max_mana", GetAttributeValue(attributeRegistry.MaxMana) },
            // Buff/debuff tracking could be implemented by inspecting the _modifiers dictionary
            { "active_buffs", new List<string>() },
            { "active_debuffs", new List<string>() }
        };
        kafkaClient.SendGameplayEvent("player_status_event", playerID, payload);
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
        kafkaClient.SendGameplayEvent("player_damage_taken_event", this.playerID, payload);
    }
}