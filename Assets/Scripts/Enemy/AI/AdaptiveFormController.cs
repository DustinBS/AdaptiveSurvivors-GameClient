// GameClient/Assets/Scripts/Enemy/AI/AdaptiveFormController.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages an elite enemy's special form adaptations in response to player behavior.
/// This controller handles the logic for switching between a defensive "Juggernaut" form
/// and an agile "Skirmisher" form. Transitions cannot be interrupted and smoothly
/// interpolates gameplay stats in sync with the visual transformation.
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public class AdaptiveFormController : MonoBehaviour
{
    // Defines the possible adaptive states of the enemy.
    private enum AdaptiveState { Normal, Juggernaut, Skirmisher }

    [Header("Transition Settings")]
    [Tooltip("The base duration of the visual transition in seconds. This is increased by fatigue.")]
    [SerializeField] private float baseTransitionDuration = 0.20f;
    [Tooltip("The color tint applied to the Juggernaut form.")]
    [SerializeField] private Color juggernautColor = new Color(1f, 0.6f, 0.6f, 1f);
    [Tooltip("The color tint applied to the Skirmisher form.")]
    [SerializeField] private Color skirmisherColor = new Color(0.6f, 0.8f, 1f, 1f);

    [Header("Fatigue Mechanic")]
    [Tooltip("How much longer (in seconds) each distinct transformation adds to the transition duration.")]
    [SerializeField] private float fatiguePenaltyPerStack = 0.1f;
    private const float FATIGUE_RECOVERY_SECONDS = 5.0f;
    private const float GRACE_PERIOD_SECONDS = 2.0f; // The duration of the spawn grace period before they are penalized.
    private readonly Queue<float> recentTransformationTimes = new Queue<float>();

    [Header("Adaptation Modifiers")]
    [SerializeField] private float juggernautScale = 3f;
    [SerializeField] private float skirmisherScale = 0.5f;

    [Header("Juggernaut (Anti-Melee)")]
    [SerializeField] private float juggernautHealthMod = 3.0f;
    [SerializeField] private float juggernautDamageMod = 3f;

    [Header("Skirmisher (Anti-Ranged)")]
    [SerializeField] private float skirmisherSpeedMod = 2f;

    [Header("Offline Fallback")]
    [Tooltip("How often (in seconds) to switch forms if no backend message is received.")]
    [SerializeField] private float offlineSwitchInterval = 5f;

    // --- Component References & State ---
    private EnemyBrain enemyBrain;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private Coroutine transitionCoroutine;
    private Coroutine offlineRoutine;
    private AdaptiveState currentState = AdaptiveState.Normal;
    private bool isTransitioning = false;
    private float initializationTime; // Tracks when the enemy was initialized.
    private bool hasReceivedKafkaMessage = false;
    private bool offlineFormIsJuggernaut = true;

    private void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
        enemyHealth = enemyBrain.Health;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("AdaptiveFormController could not find a SpriteRenderer in its children.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        enemyBrain.OnInitialized += HandleEnemyInitialized;
    }

    private void OnDisable()
    {
        if (enemyBrain != null)
        {
            enemyBrain.OnInitialized -= HandleEnemyInitialized;
        }
        StopAllCoroutines();
    }

    /// <summary>
    /// Kicks off logic once the enemy is fully initialized and records the spawn time.
    /// </summary>
    private void HandleEnemyInitialized()
    {
        initializationTime = Time.time; // Record the time to begin the grace period.
        offlineRoutine = StartCoroutine(OfflineAdaptationRoutine());
    }

    /// <summary>
    /// Public entry point for applying an adaptation from an external source (e.g., Kafka message).
    /// </summary>
    public void ApplyAdaptationFromMessage(bool adaptToMelee)
    {
        hasReceivedKafkaMessage = true;
        if (offlineRoutine != null)
        {
            StopCoroutine(offlineRoutine);
        }
        ApplyAdaptation(adaptToMelee);
    }

    private void ApplyAdaptation(bool adaptToMelee)
    {
        if (isTransitioning)
        {
            Debug.Log("Cannot adapt: A transition is already in progress.");
            return;
        }

        if (enemyBrain == null || enemyHealth == null) return;

        StartCoroutine(TransitionToFormRoutine(adaptToMelee));
    }

    private IEnumerator TransitionToFormRoutine(bool toJuggernaut)
    {
        isTransitioning = true; // Lock the state.
        enemyBrain.CanDealContactDamage = false;

        AdaptiveState targetState = toJuggernaut ? AdaptiveState.Juggernaut : AdaptiveState.Skirmisher;
        if (currentState == targetState)
        {
            // If already in the target state, just ensure damage is enabled and exit.
            isTransitioning = false;
            enemyBrain.CanDealContactDamage = true;
            yield break;
        }

        // --- 1. SETUP AND FATIGUE ---
        // (Fatigue calculation logic remains the same)
        while (recentTransformationTimes.Count > 0 && recentTransformationTimes.Peek() < Time.time - FATIGUE_RECOVERY_SECONDS)
        {
            recentTransformationTimes.Dequeue();
        }
        float currentTransitionDuration = baseTransitionDuration + (recentTransformationTimes.Count * fatiguePenaltyPerStack);
        recentTransformationTimes.Enqueue(Time.time);


        // --- 2. DEFINE START AND END STATES FOR LERPING ---
        currentState = targetState;

        // Visuals
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer.color;
        Vector3 targetScale = toJuggernaut ? Vector3.one * juggernautScale : Vector3.one * skirmisherScale;
        Color targetColor = toJuggernaut ? juggernautColor : skirmisherColor;

        // Gameplay Stats
        float startHealthMult = enemyHealth.maxHealth / enemyHealth.baseMaxHealth;
        float startDamageMult = enemyBrain.Damage / enemyBrain.baseDamage;
        float startSpeedMult = enemyBrain.MoveSpeed / enemyBrain.baseMoveSpeed;

        float targetHealthMult = toJuggernaut ? juggernautHealthMod : 1.0f;
        float targetDamageMult = toJuggernaut ? juggernautDamageMod : 1.0f;
        float targetSpeedMult = toJuggernaut ? 1.0f : skirmisherSpeedMod;

        // --- 3. PERFORM SYNCHRONIZED TRANSITION OVER TIME ---
        float elapsedTime = 0f;
        while (elapsedTime < currentTransitionDuration)
        {
            float progress = elapsedTime / currentTransitionDuration;

            // Interpolate Visuals
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            spriteRenderer.color = Color.Lerp(startColor, targetColor, progress);

            // Interpolate Gameplay Stats in sync with visuals
            enemyHealth.ApplyHealthMultiplier(Mathf.Lerp(startHealthMult, targetHealthMult, progress));
            enemyBrain.ApplyDamageMultiplier(Mathf.Lerp(startDamageMult, targetDamageMult, progress));
            enemyBrain.ApplySpeedMultiplier(Mathf.Lerp(startSpeedMult, targetSpeedMult, progress));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- 4. FINALIZE AND CLEAN UP ---
        // Set final values perfectly to avoid floating point inaccuracies.
        transform.localScale = targetScale;
        spriteRenderer.color = targetColor;
        enemyHealth.ApplyHealthMultiplier(targetHealthMult);
        enemyBrain.ApplyDamageMultiplier(targetDamageMult);
        enemyBrain.ApplySpeedMultiplier(targetSpeedMult);

        enemyBrain.CanDealContactDamage = true;
        isTransitioning = false; // Unlock the state.
    }

    private IEnumerator OfflineAdaptationRoutine()
    {
        yield return new WaitForSeconds(3f);
        while (!hasReceivedKafkaMessage && this.enabled)
        {
            ApplyAdaptation(offlineFormIsJuggernaut);
            offlineFormIsJuggernaut = !offlineFormIsJuggernaut;
            yield return new WaitForSeconds(offlineSwitchInterval);
        }
    }
}