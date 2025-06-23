// GameClient/Assets/Scripts/Enemy/AI/AdaptiveFormController.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the visual and stat adaptations of an elite enemy, changing its "form".
/// Includes an offline fallback mode for gameplay without a backend connection.
/// </summary>
[RequireComponent(typeof(EnemyBrain), typeof(EnemyHealth))]
public class AdaptiveFormController : MonoBehaviour
{
    [Header("Adaptation Modifiers")]
    [SerializeField] private float juggernautScale = 3f;
    [SerializeField] private float skirmisherScale = 0.5f;

    [Header("Juggernaut (Anti-Melee)")]
    [SerializeField] private float juggernautHealthMod = 10.0f;
    [SerializeField] private float juggernautDamageMod = 5f;

    [Header("Skirmisher (Anti-Ranged)")]
    [SerializeField] private float skirmisherSpeedMod = 1.75f;

    [Header("Offline Fallback")]
    [Tooltip("How often (in seconds) to switch forms if no Kafka message is received.")]
    [SerializeField] private float offlineSwitchInterval = 5f;

    private EnemyBrain enemyBrain;
    private EnemyHealth enemyHealth;
    private bool hasReceivedKafkaMessage = false;
    private bool offlineFormIsJuggernaut = true;

    void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        // Use Start instead of OnEnable to ensure everything is initialized.
        StartCoroutine(OfflineAdaptationRoutine());
    }

    /// <summary>
    /// The primary method to trigger an adaptation from an external source (i.e., Kafka).
    /// </summary>
    public void ApplyAdaptationFromMessage(bool adaptToMelee)
    {
        // The first time this is called, we know we have a backend connection.
        hasReceivedKafkaMessage = true;
        // Now delegate to the actual logic.
        ApplyAdaptation(adaptToMelee);
    }

    private void ApplyAdaptation(bool adaptToMelee)
    {
        if (enemyBrain == null || enemyHealth == null) return;

        enemyBrain.ResetStatMultipliers();

        if (adaptToMelee) BecomeJuggernaut();
        else BecomeSkirmisher();
    }

    private void BecomeJuggernaut()
    {
        Debug.Log($"{gameObject.name} is adapting into a Juggernaut (Anti-Melee)!", this);
        transform.localScale = Vector3.one * juggernautScale;
        enemyBrain.ApplyDamageMultiplier(juggernautDamageMod);
        enemyHealth.maxHealth *= juggernautHealthMod;
        enemyHealth.currentHealth = enemyHealth.maxHealth;
    }

    private void BecomeSkirmisher()
    {
        Debug.Log($"{gameObject.name} is adapting into a Skirmisher (Anti-Ranged)!", this);
        transform.localScale = Vector3.one * skirmisherScale;
        enemyBrain.ApplySpeedMultiplier(skirmisherSpeedMod);
    }

    /// <summary>
    /// This coroutine runs in the background. If no Kafka message is ever received,
    /// it will periodically switch the enemy's form to keep gameplay dynamic.
    /// </summary>
    private IEnumerator OfflineAdaptationRoutine()
    {
        yield return new WaitForSeconds(3f);

        while (!hasReceivedKafkaMessage)
        {
            Debug.LogWarning($"Offline Fallback: Switching adaptive form for {gameObject.name}.", this);
            ApplyAdaptation(offlineFormIsJuggernaut);

            offlineFormIsJuggernaut = !offlineFormIsJuggernaut;

            yield return new WaitForSeconds(offlineSwitchInterval);
        }
    }
}
