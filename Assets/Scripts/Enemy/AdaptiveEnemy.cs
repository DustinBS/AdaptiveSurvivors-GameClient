// GameClient/Assets/Scripts/Enemy/AdaptiveEnemy.cs

using UnityEngine;

/// <summary>
/// A simple bridge between Kafka messages and the AdaptiveFormController.
/// </summary>
[RequireComponent(typeof(AdaptiveFormController))]
public class AdaptiveEnemy : MonoBehaviour
{
    private AdaptiveFormController formController;

    void Awake()
    {
        formController = GetComponent<AdaptiveFormController>();
    }

    void OnEnable()
    {
        KafkaClient.onAdaptiveParametersReceived += OnAdaptiveParametersReceived;
    }

    void OnDisable()
    {
        KafkaClient.onAdaptiveParametersReceived -= OnAdaptiveParametersReceived;
    }

    private void OnAdaptiveParametersReceived(KafkaClient.AdaptiveParameters parameters)
    {
        if (formController == null) return;

        // Only process messages that explicitly contain an adaptation_type.
        // This prevents messages from other systems (like the Flink job)
        // from causing unintended state changes.
        if (string.IsNullOrEmpty(parameters.adaptation_type))
        {
            return;
        }

        bool adaptToMelee = parameters.adaptation_type == "juggernaut";
        formController.ApplyAdaptationFromMessage(adaptToMelee);
    }

    public float GetDamageResistance(string weaponId)
    {
        return 0f;
    }
}
