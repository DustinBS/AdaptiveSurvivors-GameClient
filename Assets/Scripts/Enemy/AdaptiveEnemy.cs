// GameClient/Assets/Scripts/Enemy/AdaptiveEnemy.cs

using UnityEngine;
using Newtonsoft.Json;

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
        KafkaClient.OnAdaptiveMessageReceived += OnAdaptiveMessageReceived;
    }

    void OnDisable()
    {
        KafkaClient.OnAdaptiveMessageReceived -= OnAdaptiveMessageReceived;
    }

    private void OnAdaptiveMessageReceived(KafkaClient.AdaptiveMessageEnvelope envelope)
    {
        // --- ROUTING LOGIC ---
        // Only process messages specifically intended for form adaptation.
        if (envelope.message_type != "form_adaptation")
        {
            return;
        }

        try
        {
            // Deserialize the payload string into the specific payload object.
            var payload = JsonConvert.DeserializeObject<KafkaClient.FormAdaptationPayload>(envelope.payload);

            if (formController != null)
            {
                // Debug.Log($"Received {envelope.message_type} with adaptation {payload.adaptation_type}");
                bool adaptToMelee = payload.adaptation_type == "juggernaut";
                formController.ApplyAdaptationFromMessage(adaptToMelee);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to deserialize FormAdaptationPayload: {e.Message}\nPayload: {envelope.payload}");
        }
    }
}
