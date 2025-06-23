// GameClient/Assets/Scripts/Enemy/AI/AdaptiveFormController.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// Manages special form adaptations for an elite enemy. Subscribes to the EnemyBrain's
/// OnInitialized event to guarantee safe execution and preserves health percentage
/// during transformations.
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public class AdaptiveFormController : MonoBehaviour
{
    [Header("Adaptation Modifiers")]
    [SerializeField] private float juggernautScale = 2f;
    [SerializeField] private float skirmisherScale = 0.7f;

    [Header("Juggernaut (Anti-Melee)")]
    [SerializeField] private float juggernautHealthMod = 2.0f;
    [SerializeField] private float juggernautDamageMod = 5f;

    [Header("Skirmisher (Anti-Ranged)")]
    [SerializeField] private float skirmisherSpeedMod = 1.75f;

    [Header("Offline Fallback")]
    [Tooltip("How often (in seconds) to switch forms if no backend message is received.")]
    [SerializeField] private float offlineSwitchInterval = 5f;

    private EnemyBrain enemyBrain;
    private EnemyHealth enemyHealth;
    private bool hasReceivedKafkaMessage = false;
    private bool offlineFormIsJuggernaut = true;
    private Coroutine offlineRoutine;

    private void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
        enemyHealth = enemyBrain.Health; // Relies on EnemyBrain to provide this reference.
    }

    private void OnEnable()
    {
        // Subscribe to the brain's event to know when it's safe to start logic.
        enemyBrain.OnInitialized += HandleEnemyInitialized;
    }

    private void OnDisable()
    {
        // Always unsubscribe from events to prevent memory leaks and errors.
        if (enemyBrain != null)
        {
            enemyBrain.OnInitialized -= HandleEnemyInitialized;
        }
        // Ensure the coroutine is stopped if the object is disabled or destroyed.
        if (offlineRoutine != null)
        {
            StopCoroutine(offlineRoutine);
        }
    }

    /// <summary>
    /// Event handler called by EnemyBrain once it's fully initialized.
    /// This is the safe entry point for this component's logic.
    /// </summary>
    private void HandleEnemyInitialized()
    {
        offlineRoutine = StartCoroutine(OfflineAdaptationRoutine());
    }

    /// <summary>
    /// Primary entry point for an external system (e.g., Kafka) to trigger an adaptation.
    /// </summary>
    /// <param name="adaptToMelee">True to become Juggernaut, false for Skirmisher.</param>
    public void ApplyAdaptationFromMessage(bool adaptToMelee)
    {
        hasReceivedKafkaMessage = true;
        if (offlineRoutine != null)
        {
            StopCoroutine(offlineRoutine); // Backend message overrides the offline fallback.
        }
        ApplyAdaptation(adaptToMelee);
    }

    private void ApplyAdaptation(bool adaptToMelee)
    {
        if (enemyBrain == null || enemyHealth == null) return;

        enemyBrain.ResetStatMultipliers();

        if (adaptToMelee)
        {
            BecomeJuggernaut();
        }
        else
        {
            BecomeSkirmisher();
        }
    }

    private void BecomeJuggernaut()
    {
        transform.localScale = Vector3.one * juggernautScale;
        enemyBrain.ApplyDamageMultiplier(juggernautDamageMod);

        // --- Percentage-Based Health Update ---
        // This preserves the "damage taken" ratio when max health changes.
        float healthPercent = enemyHealth.currentHealth / enemyHealth.maxHealth;
        float newMaxHealth = enemyHealth.maxHealth * juggernautHealthMod;
        enemyHealth.maxHealth = newMaxHealth;
        enemyHealth.currentHealth = newMaxHealth * healthPercent;
    }

    private void BecomeSkirmisher()
    {
        transform.localScale = Vector3.one * skirmisherScale;
        enemyBrain.ApplySpeedMultiplier(skirmisherSpeedMod);
    }

    /// <summary>
    /// A fallback coroutine that periodically adapts the enemy if no
    /// external message is ever received, ensuring dynamic gameplay offline.
    /// </summary>
    private IEnumerator OfflineAdaptationRoutine()
    {
        // Initial delay to allow the enemy to fully appear on screen.
        yield return new WaitForSeconds(3f);

        while (!hasReceivedKafkaMessage && this.enabled)
        {
            ApplyAdaptation(offlineFormIsJuggernaut);
            offlineFormIsJuggernaut = !offlineFormIsJuggernaut; // Toggle for next adaptation.
            yield return new WaitForSeconds(offlineSwitchInterval);
        }
    }
}