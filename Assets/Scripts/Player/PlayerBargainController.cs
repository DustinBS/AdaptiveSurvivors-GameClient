// GameClient/Assets/Scripts/Player/PlayerBargainController.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the application and removal of temporary buffs and debuffs from the Ethereal Seer's bargains.
/// This component centralizes all modifier logic, keeping other player scripts clean.
/// </summary>
public class PlayerBargainController : MonoBehaviour
{
    // References to the components that will be modified.
    private PlayerStatus playerStatus;
    private PlayerAttack playerAttack;
    private PlayerMovement playerMovement;

    void Awake()
    {
        // Cache references for performance.
        playerStatus = GetComponent<PlayerStatus>();
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// Applies a list of bargain effects (buffs or debuffs) to the player.
    /// </summary>
    /// <param name="effects">The list of effects to apply.</param>
    public void ApplyBargainEffects(List<BargainEffect> effects)
    {
        foreach (var effect in effects)
        {
            StartCoroutine(ApplyEffectRoutine(effect));
        }
    }

    private IEnumerator ApplyEffectRoutine(BargainEffect effect)
    {
        // Apply the effect immediately.
        ModifyStat(effect.targetStat, effect.modifier, effect.isPercentage);

        // Wait for the specified duration.
        if (effect.duration > 0)
        {
            yield return new WaitForSeconds(effect.duration);

            // Revert the effect by applying the inverse modification.
            ModifyStat(effect.targetStat, -effect.modifier, effect.isPercentage);
        }
        // If duration is 0 or less, the effect is permanent for the run.
    }

    private void ModifyStat(BargainTargetStat target, float value, bool isPercentage)
    {
        switch (target)
        {
            // PlayerStatus Mods
            case BargainTargetStat.MaxHealth:
                playerStatus.ModifyMaxHealth(value, isPercentage);
                break;
            case BargainTargetStat.Armor:
                playerStatus.ModifyArmor(value);
                break;

            // PlayerMovement Mods
            case BargainTargetStat.MoveSpeed:
                playerMovement.ModifyMoveSpeed(value, isPercentage);
                break;
            case BargainTargetStat.DashCooldown:
                playerMovement.ModifyDashCooldown(value, isPercentage);
                break;

            // PlayerAttack Mods
            case BargainTargetStat.AttackDamage:
                playerAttack.ModifyDamage(value, isPercentage);
                break;
            case BargainTargetStat.AttackSpeed:
                playerAttack.ModifyAttackSpeed(value, isPercentage);
                break;

            default:
                Debug.LogWarning($"Bargain effect for '{target}' is not implemented.");
                break;
        }
    }
}