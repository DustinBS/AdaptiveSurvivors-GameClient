// GameClient/Assets/Scripts/Player/PlayerBargainController.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the application and removal of temporary buffs and debuffs from the Ethereal Seer's bargains.
/// This component acts as an "Adapter", translating incoming BargainEffect data into the internal Attribute System.
/// </summary>
public class PlayerBargainController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("A reference to the AttributeRegistry asset. Used to map bargain stats to game attributes.")]
    [SerializeField] private AttributeRegistry attributeRegistry;

    // It now only needs a single reference to the central stats manager.
    private PlayerStats playerStats;

    void Awake()
    {
        // Cache the reference to the single source of truth for player stats.
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerBargainController could not find a PlayerStats component!", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Applies a list of bargain effects (buffs or debuffs) to the player.
    /// </summary>
    /// <param name="effects">The list of effects to apply, typically from a Seer bargain.</param>
    public void ApplyBargainEffects(List<BargainEffect> effects)
    {
        foreach (var effect in effects)
        {
            StartCoroutine(ApplyEffectRoutine(effect));
        }
    }

    private IEnumerator ApplyEffectRoutine(BargainEffect effect)
    {
        // 1. Translate the BargainTargetStat enum to a game AttributeData object.
        AttributeData targetAttribute = GetAttributeFromBargainStat(effect);

        if (targetAttribute == null)
        {
            Debug.LogWarning($"No AttributeData mapping found for BargainTargetStat: {effect.targetStat}");
            yield break; // Stop processing this effect if no attribute is found.
        }

        // 2. Create the modifier, using the 'effect' object itself as the unique source ID.
        var modifier = new AttributeModifier(effect.modifier, effect.isPercentage, effect);

        // 3. Apply the modifier to the player.
        playerStats.AddModifier(targetAttribute, modifier);

        // 4. If the duration is positive, wait and then remove the specific modifier.
        if (effect.duration > 0)
        {
            yield return new WaitForSeconds(effect.duration);

            // 5. Remove the exact modifier we added by referencing its source.
            playerStats.RemoveModifier(targetAttribute, effect);
        }
        // If duration is 0 or less, the effect is permanent for the run and is never removed.
    }

    /// <summary>
    /// Maps the incoming BargainTargetStat enum to the corresponding AttributeData ScriptableObject.
    /// This version intelligently routes flat vs. percentage bonuses to the correct attribute
    /// (e.g., flat damage modifies BaseDamage, percentage damage modifies GlobalDamageMultiplier).
    /// </summary>
    private AttributeData GetAttributeFromBargainStat(BargainEffect effect)
    {
        switch (effect.targetStat)
        {
            case BargainTargetStat.MaxHealth:
                // Max Health is a base stat, so both flat and percentage bonuses apply to it directly.
                return attributeRegistry.MaxHealth;

            case BargainTargetStat.Armor:
                return attributeRegistry.Armor;

            case BargainTargetStat.MoveSpeed:
                return attributeRegistry.MoveSpeed;

            case BargainTargetStat.AttackDamage:
                // If it's a percentage, modify the global multiplier. If it's flat, modify the base.
                return effect.isPercentage ? attributeRegistry.GlobalDamageMultiplier : attributeRegistry.BaseDamage;

            case BargainTargetStat.AttackSpeed:
                // Likewise for Attack Speed.
                return effect.isPercentage ? attributeRegistry.GlobalAttackSpeedMultiplier : attributeRegistry.BaseAttackSpeed;
            // Note: DashCooldown is not yet an attribute in our registry.
            // To implement this, you would add a DashCooldown AttributeData to the registry
            // and have PlayerMovement read from it. For now, we'll leave it out.
            // case BargainTargetStat.DashCooldown:
            //     return attributeRegistry.DashCooldown;
            default:
                Debug.LogWarning($"No AttributeData mapping found for BargainTargetStat: {effect.targetStat}");
                return null;
        }
    }
}