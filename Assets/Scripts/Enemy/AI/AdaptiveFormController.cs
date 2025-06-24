// GameClient/Assets/Scripts/Enemy/AI/AdaptiveFormController.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// Manages special form adaptations for an elite enemy. Now features a smooth,
/// animated transition between forms for better visual feedback.
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public class AdaptiveFormController : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("How long the visual transition between forms takes in seconds.")][SerializeField]
    private float transitionDuration = 0.25f;
    [Tooltip("The color tint applied to the Juggernaut form.")][SerializeField]
    private Color juggernautColor = new Color(1f, 0.6f, 0.6f, 1f); // A reddish tint
    [Tooltip("The color tint applied to the Skirmisher form.")][SerializeField]
    private Color skirmisherColor = new Color(0.6f, 0.8f, 1f, 1f); // A bluish tint

    [Header("Adaptation Modifiers")]
    [SerializeField] private float juggernautScale = 3f;
    [SerializeField] private float skirmisherScale = 0.7f;

    [Header("Juggernaut (Anti-Melee)")]
    [SerializeField] private float juggernautHealthMod = 2.0f;
    [SerializeField] private float juggernautDamageMod = 5f;

    [Header("Skirmisher (Anti-Ranged)")]
    [SerializeField] private float skirmisherSpeedMod = 1.75f;

    [Header("Offline Fallback")]
    [Tooltip("How often (in seconds) to switch forms if no backend message is received.")][SerializeField]
    private float offlineSwitchInterval = 5f;

    private EnemyBrain enemyBrain;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer; // NEW: Reference to the sprite renderer for color tinting.
    private Coroutine transitionCoroutine;
    private Coroutine offlineRoutine;
    private bool hasReceivedKafkaMessage = false;
    private bool offlineFormIsJuggernaut = true;

    private void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
        enemyHealth = enemyBrain.Health;
        // Get the renderer from children to allow for more complex prefabs.
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
        StopAllCoroutines(); // Safely stop all coroutines on disable
    }

    private void HandleEnemyInitialized()
    {
        offlineRoutine = StartCoroutine(OfflineAdaptationRoutine());
    }

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
        if (enemyBrain == null || enemyHealth == null) return;

        // Stop any existing transition before starting a new one.
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionToFormRoutine(adaptToMelee));
    }

    /// <summary>
    /// A coroutine that smoothly transitions the enemy's scale and color to the target form.
    /// </summary>
    private IEnumerator TransitionToFormRoutine(bool toJuggernaut)
    {
        // --- 1. Apply Gameplay Stat Changes INSTANTLY ---
        enemyBrain.ResetStatMultipliers();

        Vector3 targetScale;
        Color targetColor;

        if (toJuggernaut)
        {
            targetScale = Vector3.one * juggernautScale;
            targetColor = juggernautColor;

            // Apply Juggernaut stats
            enemyBrain.ApplyDamageMultiplier(juggernautDamageMod);
            float healthPercent = enemyHealth.currentHealth / enemyHealth.maxHealth;
            float newMaxHealth = enemyBrain.Health.maxHealth * juggernautHealthMod;
            enemyHealth.maxHealth = newMaxHealth;
            enemyHealth.currentHealth = newMaxHealth * healthPercent;
        }
        else
        {
            targetScale = Vector3.one * skirmisherScale;
            targetColor = skirmisherColor;

            // Apply Skirmisher stats
            enemyBrain.ApplySpeedMultiplier(skirmisherSpeedMod);
        }

        // --- 2. Perform Visual Transition Over Time ---
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer.color;
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float progress = elapsedTime / transitionDuration;

            // Smoothly interpolate scale and color
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, targetColor, progress);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- 3. Finalize Visuals ---
        // Ensure the final values are set perfectly.
        transform.localScale = targetScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
        }

        transitionCoroutine = null;
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