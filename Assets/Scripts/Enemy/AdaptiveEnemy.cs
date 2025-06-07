// GameClient/Assets/Scripts/Enemy/AdaptiveEnemy.cs

using UnityEngine;
using System.Collections.Generic;

// This script applies adaptive parameters received from Kafka to an enemy's behavior.
// It assumes the enemy has an EnemyHealth component and potentially other behavior scripts
// that can be influenced by these parameters (e.g., a movement script, an attack script).
// Attach this script to "Elite" enemy GameObjects that are meant to adapt.

public class AdaptiveEnemy : MonoBehaviour
{
    [Header("Adaptive Enemy Settings")]
    [Tooltip("Unique identifier for this enemy instance. Should match EnemyHealth's EnemyId.")]
    public string enemyId;

    [Tooltip("Reference to the enemy's EnemyHealth component.")]
    public EnemyHealth enemyHealth; // Drag and drop in Inspector

    [Tooltip("Base movement speed of the enemy.")]
    public float baseMoveSpeed = 3f;
    private float currentMoveSpeed;

    [Tooltip("Base attack damage of the enemy.")]
    public float baseAttackDamage = 5f;
    private float currentAttackDamage;

    // Internal state to track current applied adaptations
    private Dictionary<string, double> currentEnemyResistances = new Dictionary<string, double>();
    private string currentEliteBehaviorShift = "none";
    private HashSet<string> currentEliteStatusImmunities = new HashSet<string>();
    private Dictionary<string, string> currentBreakableObjectBuffsDebuffs = new Dictionary<string, string>();

    void Awake()
    {
        // Ensure EnemyHealth is attached
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                Debug.LogError($"AdaptiveEnemy: EnemyHealth component not found on {gameObject.name}. Please add one or assign it in Inspector.", this);
                enabled = false;
                return;
            }
        }

        // Ensure enemyId matches EnemyHealth's ID
        if (string.IsNullOrEmpty(enemyId) || enemyId == "enemy_001") // Also check for default
        {
            enemyId = enemyHealth.EnemyId; // Use the ID from EnemyHealth
        }
        else if (enemyId != enemyHealth.EnemyId)
        {
            Debug.LogWarning($"AdaptiveEnemy: EnemyId on this script ({enemyId}) does not match EnemyHealth's EnemyId ({enemyHealth.EnemyId}). Using EnemyHealth's ID.", this);
            enemyId = enemyHealth.EnemyId;
        }

        currentMoveSpeed = baseMoveSpeed;
        currentAttackDamage = baseAttackDamage;
    }

    void OnEnable()
    {
        // Subscribe to the KafkaClient's event for receiving adaptive parameters
        KafkaClient.onAdaptiveParametersReceived += OnAdaptiveParametersReceived;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks or errors when the GameObject is destroyed
        KafkaClient.onAdaptiveParametersReceived -= OnAdaptiveParametersReceived;
    }

    /// <summary>
    /// Callback function when adaptive parameters are received from Kafka.
    /// This method runs on the Unity main thread via UnityMainThreadDispatcher.
    /// </summary>
    /// <param name="parameters">The AdaptiveParameters object containing new adaptations.</param>
    private void OnAdaptiveParametersReceived(KafkaClient.AdaptiveParameters parameters)
    {
        // For a single-player game, we assume these parameters apply to all relevant enemies.
        // If it were a multiplayer game, you'd check `parameters.playerId` to apply
        // adaptations specific to that player's influence.

        // Commented Debug.Log($"AdaptiveEnemy: Received new adaptive parameters for player {parameters.playerId}. Applying adaptations to {enemyId}.");

        // Apply Enemy Resistances
        currentEnemyResistances = parameters.enemyResistances;
        ApplyResistances();

        // Apply Elite Behavior Shift
        currentEliteBehaviorShift = parameters.eliteBehaviorShift;
        ApplyBehaviorShift();

        // Apply Elite Status Immunities
        currentEliteStatusImmunities = parameters.eliteStatusImmunities;
        ApplyStatusImmunities();

        // Apply Breakable Object Buffs/Debuffs (if this enemy interacts with them)
        // This part needs more context on how breakable objects influence enemies
        currentBreakableObjectBuffsDebuffs = parameters.breakableObjectBuffsDebuffs;
        ApplyBreakableObjectEffects();
    }

    /// <summary>
    /// Applies damage resistances based on received parameters.
    /// This method would typically be called by the `EnemyHealth` script when taking damage,
    /// so `EnemyHealth` would need a reference to `AdaptiveEnemy` or a way to query it for resistances.
    /// For now, we'll just log it.
    /// </summary>
    private void ApplyResistances()
    {
        if (currentEnemyResistances.Count > 0)
        {
            string resistances = "";
            foreach (var entry in currentEnemyResistances)
            {
                resistances += $"{entry.Key}: {entry.Value * 100}% ";
            }
            // Commented Debug.Log($"Enemy {enemyId} now has resistances: {resistances}");
            // Example: If PlayerAttack knows the weaponId, it can query EnemyHealth for effective damage.
            // Or EnemyHealth itself can consult this script.
        }
    }

    /// <summary>
    /// Applies behavioral shifts to the enemy.
    /// This would typically modify parameters used by the enemy's movement or attack scripts.
    /// </summary>
    private void ApplyBehaviorShift()
    {
        switch (currentEliteBehaviorShift)
        {
            case "anticipate_left":
            case "anticipate_right":
            case "anticipate_forward":
            case "anticipate_backward":
                // Commented Debug.Log($"Enemy {enemyId}: Will try to anticipate player's dodge direction: {currentEliteBehaviorShift.Replace("anticipate_", "")}");
                // Implement logic in enemy AI to move towards anticipated player position.
                break;
            case "speed_aggression_boost":
                currentMoveSpeed = baseMoveSpeed * 1.5f; // 50% speed increase
                currentAttackDamage = baseAttackDamage * 1.2f; // 20% damage increase
                // Commented Debug.Log($"Enemy {enemyId}: Gained speed and aggression boost! New Speed: {currentMoveSpeed}, New Damage: {currentAttackDamage}");
                // Update enemy's movement and attack components
                break;
            case "temporary_slow_on_hit":
                // Commented Debug.Log($"Enemy {enemyId}: Now applies a temporary slow on hit.");
                // Implement logic in enemy's attack script to apply a slow debuff.
                break;
            case "none":
                currentMoveSpeed = baseMoveSpeed;
                currentAttackDamage = baseAttackDamage;
                // Reset to base
                // Commented Debug.Log($"Enemy {enemyId}: Behavior shift reset to none. Speed: {currentMoveSpeed}, Damage: {currentAttackDamage}");
                break;
            default:
                // Commented Debug.Log($"Enemy {enemyId}: Unknown behavior shift: {currentEliteBehaviorShift}");
                break;
        }

        // Example of applying speed: If enemy has a separate movement script
        // var enemyMovement = GetComponent<EnemyMovement>(); // Hypothetical EnemyMovement script
        // if (enemyMovement != null) enemyMovement.SetSpeed(currentMoveSpeed);
    }

    /// <summary>
    /// Applies status immunities to the enemy.
    /// This would typically be checked by other scripts trying to apply status effects.
    /// </summary>
    private void ApplyStatusImmunities()
    {
        if (currentEliteStatusImmunities.Count > 0)
        {
            string immunities = string.Join(", ", currentEliteStatusImmunities);
            // Commented Debug.Log($"Enemy {enemyId} is now immune to: {immunities}");
            // Implement logic to prevent status effects from being applied (e.g., in a StatusEffectManager).
        }
    }

    /// <summary>
    /// Applies effects related to breakable objects.
    /// This part of the adaptation logic is more complex as it depends on
    /// how enemies might react to player's interaction with the environment.
    /// For this POC, we'll just log the effect.
    /// </summary>
    private void ApplyBreakableObjectEffects()
    {
        if (currentBreakableObjectBuffsDebuffs.Count > 0)
        {
            foreach (var entry in currentBreakableObjectBuffsDebuffs)
            {
                // Commented Debug.Log($"Enemy {enemyId} affected by breakable object effect from '{entry.Key}': {entry.Value}");
                // Further logic here to interpret and apply the effect, e.g., temporary buffs/debuffs on the enemy.
            }
        }
    }

    // You can add methods here that other enemy components might call to check for adaptations.
    // For example, an enemy attack script could call IsImmuneToStatus("freeze").
    public bool IsImmuneToStatus(string statusEffect)
    {
        return currentEliteStatusImmunities.Contains(statusEffect);
    }

    public float GetWeaponDamageResistance(string weaponId)
    {
        // Explicitly cast to float to resolve the error
        return (float)currentEnemyResistances.GetValueOrDefault(weaponId, 0.0);
    }
}
