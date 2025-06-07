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
    private UnityMainThreadDispatcher mainThreadDispatcher;

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
        // Find the dispatcher on the main thread and cache it for later use.
        // This is a safe operation because Awake() is always on the main thread.
        mainThreadDispatcher = FindAnyObjectByType<UnityMainThreadDispatcher>();
        if (mainThreadDispatcher == null)
        {
            Debug.LogError("KafkaClient: UnityMainThreadDispatcher not found in the scene! Messages from Kafka will not be processed.");
            // Optionally disable the component if the dispatcher is essential
            enabled = false;
            return;
        }

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
    /// Initializes the Kafka Producer with detailed internal logging enabled for diagnostics.
    /// </summary>
    private void InitializeProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            // Enable all client-level debugging messages from the underlying librdkafka library.
            // This is the key change for diagnosing the silent network issue.
            Debug = "all"
        };

        try
        {
            var producerBuilder = new ProducerBuilder<string, string>(config);

            // Set a log handler to redirect the internal librdkafka logs to the Unity console.
            producerBuilder.SetLogHandler((_, logMessage) =>
            {
                // This callback will be invoked for each internal log message from the client.
                // We can now see connection attempts, failures, and other network-level details.
                // Note: This logs from a background thread. Debug.Log is thread-safe.
                // Commented Debug.Log($"[KafkaClient-Internal] {logMessage.Level} | {logMessage.Facility}: {logMessage.Message}");
            });

            producer = producerBuilder.Build();
            // Commented Debug.Log($"Kafka Producer initialized successfully for topic: {gameplayEventsTopic}");
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
            // Commented Debug.Log($"Kafka Consumer initialized and subscribed to topic: {adaptiveParamsTopic}");
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
        // Commented Debug.Log("Kafka Consumer thread started.");
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

                    // Use the PRE-CACHED reference to the dispatcher.
                    // This avoids calling .Instance() and searching from the background thread.
                    if (mainThreadDispatcher != null)
                    {
                        mainThreadDispatcher.Enqueue(() => ProcessConsumedMessage(message));
                    }
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
            // Commented Debug.Log("Signaled Kafka Consumer thread to stop.");
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
        // Flush the producer to ensure all buffered messages are sent before exiting.
        // A timeout is provided to prevent the application from hanging indefinitely.
        if (producer != null)
        {
            // Commented Debug.Log("Flushing Kafka Producer...");
            producer.Flush(TimeSpan.FromSeconds(10)); // Wait up to 10 seconds
            // Commented Debug.Log("Kafka Producer flushed.");
            producer.Dispose();
        }

        consumer?.Dispose();
        // Commented Debug.Log("Kafka Producer and Consumer disposed.");
    }

    /// <summary>
    /// Sends a gameplay event to Kafka using a callback handler for reliability.
    /// </summary>
    /// <param name="eventType">Type of the event (e.g., "player_movement_event").</param>
    /// <param name="playerId">ID of the player.</param>
    /// <param name="payload">Dictionary of event-specific data.</param>
    public void SendGameplayEvent(string eventType, string playerId, System.Collections.Generic.Dictionary<string, object> payload)
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
            // Commented Debug.Log($"Attempting to produce event '{eventType}' with payload: {jsonMessage}");

            // Use the Produce method with a delivery handler callback.
            // This avoids the complexities and silent failures of 'async void' in Unity.
            producer.Produce(gameplayEventsTopic, new Message<string, string> { Key = playerId, Value = jsonMessage },
                (deliveryReport) =>
                {
                    // This callback executes on a background thread from the Kafka client.
                    if (deliveryReport.Error.Code != ErrorCode.NoError)
                    {
                        // Debug.LogError($"[Kafka] Delivery failed for topic {deliveryReport.Topic}: {deliveryReport.Error.Reason}");
                    }
                    else
                    {
                        // Commented Debug.Log($"[Kafka] Delivered message to {deliveryReport.TopicPartitionOffset}");
                    }
                });
        }
        catch (Exception e)
        {
            // This will catch immediate errors, like the producer's internal queue being full.
            Debug.LogError($"[Kafka] Error on calling Produce: {e.Message}");
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