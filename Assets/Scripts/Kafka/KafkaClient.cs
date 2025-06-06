// GameClient/Assets/Scripts/Kafka/KafkaClient.cs

using UnityEngine;
using System;
using System.Threading;
using Confluent.Kafka;
using Newtonsoft.Json; // Assuming you'll add Newtonsoft.Json-for-Unity via UPM or as a plugin

// This script handles Kafka communication for the Unity game client.
// It acts as both a producer for gameplay events and a consumer for adaptive parameters.
// Ensure you have the Confluent.Kafka .NET client library integrated into your Unity project.
// You might need to build it for .NET Standard 2.0 or find a compatible Unity package.
// Also, ensure Newtonsoft.Json-for-Unity is installed for JSON serialization/deserialization.

public class KafkaClient : MonoBehaviour
{
    [Header("Kafka Settings")]
    [Tooltip("Comma-separated list of Kafka broker addresses (e.g., 'localhost:29092')")]
    public string bootstrapServers = "localhost:29092";

    [Tooltip("Topic to send gameplay events to")]
    public string gameplayEventsTopic = "gameplay_events";

    [Tooltip("Topic to consume adaptive parameters from")]
    public string adaptiveParamsTopic = "adaptive_params";

    [Tooltip("Consumer group ID for adaptive parameters")]
    public string consumerGroupId = "unity_game_client";

    // Kafka Producer and Consumer instances
    private IProducer<string, string> producer;
    private IConsumer<string, string> consumer;

    // Thread for background consumption to avoid blocking the main Unity thread
    private CancellationTokenSource consumeCancellationTokenSource;
    private Thread consumerThread;

    // Delegate for events received from Kafka
    public delegate void OnAdaptiveParametersReceived(AdaptiveParameters parameters);
    public static event OnAdaptiveParametersReceived onAdaptiveParametersReceived;

    // JSON serialization settings (optional, for readability)
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None // Compact JSON
    };

    void Awake()
    {
        InitializeProducer();
        InitializeConsumer();
    }

    void OnEnable()
    {
        StartConsumerThread();
    }

    void OnDisable()
    {
        StopConsumerThread();
        CleanUpKafkaClients();
    }

    /// <summary>
    /// Initializes the Kafka Producer.
    /// </summary>
    private void InitializeProducer()
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };

        try
        {
            producer = new ProducerBuilder<string, string>(config).Build();
            Debug.Log($"Kafka Producer initialized successfully for topic: {gameplayEventsTopic}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Kafka Producer: {e.Message}");
        }
    }

    /// <summary>
    /// Initializes the Kafka Consumer.
    /// </summary>
    private void InitializeConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Latest // Start consuming from the latest message
        };

        try
        {
            consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(adaptiveParamsTopic);
            Debug.Log($"Kafka Consumer initialized and subscribed to topic: {adaptiveParamsTopic}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Kafka Consumer: {e.Message}");
        }
    }

    /// <summary>
    /// Starts a background thread for Kafka message consumption.
    /// This prevents blocking the main Unity thread.
    /// </summary>
    /// <param name="cancellationToken">Token to signal cancellation of the thread.</param>
    private void StartConsumerThread()
    {
        if (consumer == null)
        {
            Debug.LogError("Consumer not initialized. Cannot start consumer thread.");
            return;
        }

        consumeCancellationTokenSource = new CancellationTokenSource();
        consumerThread = new Thread(() => ConsumeMessages(consumeCancellationTokenSource.Token));
        consumerThread.IsBackground = true; // Allow the application to exit even if thread is running
        consumerThread.Start();
        Debug.Log("Kafka Consumer thread started.");
    }

    /// <summary>
    /// Consumes messages from Kafka in a background thread.
    /// </summary>
    /// <param name="cancellationToken">Token to signal cancellation of the thread.</param>
    private void ConsumeMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);
                if (consumeResult != null && consumeResult.Message != null)
                {
                    string message = consumeResult.Message.Value;
                    Debug.Log($"Consumed message from Kafka: {message}");
                    // Use Unity's main thread dispatcher to process the message on the main thread
                    UnityMainThreadDispatcher.Instance().Enqueue(() => ProcessConsumedMessage(message));
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Kafka Consumer thread cancelled.");
                break;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error consuming Kafka message: {e.Message}");
                // Add a small delay to prevent rapid error logging in case of persistent issues
                Thread.Sleep(1000);
            }
        }
    }

    /// <summary>
    /// Processes a consumed Kafka message (deserializes and invokes event).
    /// Executed on the Unity main thread.
    /// </summary>
    /// <param name="message">The JSON string message from Kafka.</param>
    private void ProcessConsumedMessage(string message)
    {
        try
        {
            AdaptiveParameters parameters = JsonConvert.DeserializeObject<AdaptiveParameters>(message);
            onAdaptiveParametersReceived?.Invoke(parameters); // Invoke the event for subscribers
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deserializing adaptive parameters: {e.Message}\nMessage: {message}");
        }
    }

    /// <summary>
    /// Stops the Kafka consumer background thread.
    /// </summary>
    private void StopConsumerThread()
    {
        if (consumeCancellationTokenSource != null)
        {
            consumeCancellationTokenSource.Cancel();
            Debug.Log("Signaled Kafka Consumer thread to stop.");
        }

        if (consumerThread != null && consumerThread.IsAlive)
        {
            consumerThread.Join(TimeSpan.FromSeconds(5)); // Wait for the thread to finish
            if (consumerThread.IsAlive)
            {
                Debug.LogWarning("Kafka Consumer thread did not terminate gracefully.");
                // Optionally, forcefully abort the thread if it's still alive (use with caution)
                // consumerThread.Abort();
            }
        }
    }

    /// <summary>
    /// Cleans up and disposes of Kafka producer and consumer clients.
    /// </summary>
    private void CleanUpKafkaClients()
    {
        producer?.Dispose();
        consumer?.Dispose();
        Debug.Log("Kafka Producer and Consumer disposed.");
    }

    /// <summary>
    /// Sends a gameplay event to Kafka.
    /// </summary>
    /// <param name="eventType">Type of the event (e.g., "player_movement_event").</param>
    /// <param name="playerId">ID of the player.</param>
    /// <param name="payload">Dictionary of event-specific data.</param>
    public async void SendGameplayEvent(string eventType, string playerId, System.Collections.Generic.Dictionary<string, object> payload)
    {
        if (producer == null)
        {
            Debug.LogError("Kafka Producer is not initialized. Cannot send event.");
            return;
        }

        var gameplayEvent = new GameplayEvent
        {
            event_type = eventType,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            player_id = playerId,
            payload = payload
        };

        string jsonMessage = JsonConvert.SerializeObject(gameplayEvent, JsonSettings);

        try
        {
            var deliveryReport = await producer.ProduceAsync(gameplayEventsTopic, new Message<string, string> { Key = playerId, Value = jsonMessage });
            // Debug.Log($"Delivered message to {deliveryReport.TopicPartitionOffset}");
        }
        catch (ProduceException<string, string> e)
        {
            Debug.LogError($"Delivery failed for message '{e.DeliveryResult.Value}': {e.Error.Reason}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending Kafka event: {e.Message}");
        }
    }

    // --- Data Models (Mirroring Flink Job's data models) ---

    [Serializable]
    public class GameplayEvent
    {
        public string event_type;
        public long timestamp;
        public string player_id;
        public System.Collections.Generic.Dictionary<string, object> payload;
    }

    [Serializable]
    public class AdaptiveParameters
    {
        public string playerId;
        public System.Collections.Generic.Dictionary<string, double> enemyResistances;
        public string eliteBehaviorShift;
        public System.Collections.Generic.HashSet<string> eliteStatusImmunities; // Using HashSet for sets
        public System.Collections.Generic.Dictionary<string, string> breakableObjectBuffsDebuffs;
        public long timestamp;
    }
}