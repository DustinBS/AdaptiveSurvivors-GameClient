// GameClient/Assets/Scripts/Kafka/KafkaClient.cs

using UnityEngine;
using System;
using System.Threading;
using Confluent.Kafka;
using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// Persistent global service for Kafka communication.
/// Its persistence is managed by the PersistentManagers prefab.
/// </summary>
public class KafkaClient : MonoBehaviour
{
    // --- Public Instance ---
    // Global access to the single KafkaClient instance.
    public static KafkaClient Instance { get; private set; }

    [Header("Kafka Settings")]
    [Tooltip("Comma-separated list of Kafka broker addresses (e.g., 'localhost:29092')")]
    public string bootstrapServers = "localhost:29092";
    [Tooltip("Topic to send gameplay events to")]
    public string gameplayEventsTopic = "gameplay_events";
    [Tooltip("Topic to consume adaptive parameters from")]
    public string adaptiveParamsTopic = "adaptive_params";
    [Tooltip("Topic to consume Seer results from")]
    public string seerResultsTopic = "seer_results";
    [Tooltip("Consumer group ID for adaptive parameters")]
    public string consumerGroupId = "unity_game_client";

    // Kafka Producer and Consumer instances
    private IProducer<string, string> producer;
    private IConsumer<string, string> consumer;

    // Cached reference to UnityMainThreadDispatcher for thread-safe main thread operations.
    private UnityMainThreadDispatcher mainThreadDispatcher;

    // Cancellation token and thread for background Kafka consumption.
    private CancellationTokenSource consumeCancellationTokenSource;
    private Thread consumerThread;

    // event to pass the message envelope
    public delegate void OnAdaptiveMessageReceivedDelegate(AdaptiveMessageEnvelope envelope);
    public static event OnAdaptiveMessageReceivedDelegate OnAdaptiveMessageReceived;
    public delegate void OnSeerResultReceivedDelegate(SeerResultPayload payload);
    public static event OnSeerResultReceivedDelegate OnSeerResultReceived;

    // JSON serialization settings for consistent and compact message formatting.
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None
    };

    /// <summary>
    /// Sets up the singleton instance and initializes Kafka clients.
    /// </summary>
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        mainThreadDispatcher = FindAnyObjectByType<UnityMainThreadDispatcher>();
        if (mainThreadDispatcher == null)
        {
            Debug.LogError("KafkaClient: UnityMainThreadDispatcher not found.", this);
            enabled = false;
            return;
        }

        InitializeProducer();
        InitializeConsumer();
    }

    void OnEnable() { StartConsumerThread(); }
    void OnDisable() { StopConsumerThread(); CleanUpKafkaClients(); }


    /// <summary>
    /// Initializes the Kafka Producer.
    /// </summary>
    private void InitializeProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
        };

        try
        {
            var producerBuilder = new ProducerBuilder<string, string>(config);
            producer = producerBuilder.Build();
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
            AutoOffsetReset = AutoOffsetReset.Latest,
        };

        try
        {
            var consumerBuilder = new ConsumerBuilder<string, string>(config);
            consumer = consumerBuilder.Build();
            var topics = new List<string> { adaptiveParamsTopic, seerResultsTopic };
            consumer.Subscribe(topics);
            Debug.Log($"Kafka Consumer subscribed to: {string.Join(", ", topics)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Kafka Consumer: {e.Message}");
        }
    }

    /// <summary>
    /// Starts a background thread for Kafka message consumption, preventing main thread blocking.
    /// </summary>
    private void StartConsumerThread()
    {
        if (consumer == null)
        {
            Debug.LogError("Consumer not initialized. Cannot start consumer thread.");
            return;
        }

        StopConsumerThread(); // Safely stop any existing thread before starting a new one.

        consumeCancellationTokenSource = new CancellationTokenSource();
        consumerThread = new Thread(() => ConsumeMessages(consumeCancellationTokenSource.Token));
        consumerThread.IsBackground = true;
        consumerThread.Start();
    }

    /// <summary>
    /// Consumes Kafka messages in a background thread, enqueuing them to the main thread for processing.
    /// </summary>
    /// <param name="cancellationToken">Token to signal cancellation.</param>
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
                    // Debug.Log($"Consumed message from Kafka topic '{consumeResult.Topic}': {message}");

                    // Enqueue message processing to the main thread via the cached dispatcher.
                    if (mainThreadDispatcher != null)
                    {
                        mainThreadDispatcher.Enqueue(() => ProcessConsumedMessage(message));
                    }
                    else
                    {
                        Debug.LogWarning("KafkaClient: UnityMainThreadDispatcher is null in consumer thread. Message not processed on main thread.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Kafka Consumer thread cancellation requested. Exiting consumer loop.");
                break;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error consuming Kafka message: {e.Message}");
                Thread.Sleep(1000); // Prevent busy loop on continuous errors.
            }
        }
    }

    /// <summary>
    /// Processes a consumed Kafka message (deserializes and invokes event) on the Unity main thread.
    /// </summary>
    /// <param name="message">The JSON string message from Kafka.</param>
    private void ProcessConsumedMessage(string message)
    {
        try
        {
            // First, deserialize into the generic envelope to find out the message type.
            AdaptiveMessageEnvelope envelope = JsonConvert.DeserializeObject<AdaptiveMessageEnvelope>(message);
            if (string.IsNullOrEmpty(envelope.message_type) || string.IsNullOrEmpty(envelope.payload))
            {
                Debug.LogWarning($"Consumed message is not a valid envelope: {message}");
                return;
            }

            // Invoke the event, passing the whole envelope. Subscribers are responsible
            // for parsing the specific payload based on the message_type.
            if (envelope.message_type == "seer_result_update")
            {
                SeerResultPayload seerPayload = JsonConvert.DeserializeObject<SeerResultPayload>(envelope.payload);
                OnSeerResultReceived?.Invoke(seerPayload);
            }
            else
            {
                // The original logic for adaptive enemies.
                OnAdaptiveMessageReceived?.Invoke(envelope);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deserializing message envelope: {e.Message}\nMessage: {message}");
        }
    }

    /// <summary>
    /// Stops the Kafka consumer background thread gracefully.
    /// </summary>
    private void StopConsumerThread()
    {
        if (consumeCancellationTokenSource != null)
        {
            consumeCancellationTokenSource.Cancel();
            // Debug.Log("Signaled Kafka Consumer thread to stop.");
        }

        if (consumerThread != null && consumerThread.IsAlive)
        {
            consumerThread.Join(TimeSpan.FromSeconds(5)); // Wait for thread to finish.
            if (consumerThread.IsAlive)
            {
                Debug.LogWarning("Kafka Consumer thread did not terminate gracefully within 5 seconds.");
            }
        }
        consumeCancellationTokenSource?.Dispose();
        consumeCancellationTokenSource = null;
        consumerThread = null;
    }

    /// <summary>
    /// Cleans up and disposes Kafka producer and consumer client resources.
    /// </summary>
    private void CleanUpKafkaClients()
    {
        if (producer != null)
        {
            Debug.Log("Waiting 10s for Kafka Producer to flush...");
            producer.Flush(TimeSpan.FromSeconds(10)); // Wait for messages to be sent.
            // Debug.Log("Kafka Producer flushed.");
            producer.Dispose();
            producer = null;
        }

        if (consumer != null)
        {
            consumer.Dispose();
            consumer = null;
        }
        Debug.Log("Kafka Producer and Consumer disposed.");
    }

    /// <summary>
    /// Sends a gameplay event to Kafka with a delivery report callback for reliability.
    /// </summary>
    /// <param name="eventType">Type of the event (e.g., "player_movement_event").</param>
    /// <param name="playerId">ID of the player.</param>
    /// <param name="payload">Dictionary of event-specific data.</param>
    public void SendGameplayEvent(string eventType, string playerId, Dictionary<string, object> payload)
    {
        if (producer == null)
        {
            Debug.LogError("Kafka Producer is not initialized. Cannot send event.");
            return;
        }

        // Automatically get the run_id from the GameManager singleton
        string currentRunId = (GameManager.Instance != null) ? GameManager.Instance.runId : "unknown_run";

        var gameplayEvent = new GameplayEvent
        {
            event_type = eventType,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            run_id = currentRunId, // Add the run_id to every event
            player_id = playerId,
            payload = payload
        };

        string jsonMessage = JsonConvert.SerializeObject(gameplayEvent, JsonSettings);
        // Debug.Log($"Attempting to produce event '{eventType}' with payload: {jsonMessage}");

        try
        {
            producer.Produce(gameplayEventsTopic, new Message<string, string> { Key = playerId, Value = jsonMessage },
                (deliveryReport) =>
                {
                    // Callback executes on a background thread.
                    if (deliveryReport.Error.Code != ErrorCode.NoError)
                    {
                        Debug.LogError($"[Kafka] Delivery failed for topic {deliveryReport.Topic}: {deliveryReport.Error.Reason} (Code: {deliveryReport.Error.Code})");
                    }
                    else
                    {
                        // Debug.Log($"[Kafka] Delivered message to {deliveryReport.TopicPartitionOffset}");
                    }
                });
        }
        catch (Exception e)
        {
            Debug.LogError($"[Kafka] Error on calling Produce: {e.Message}");
        }
    }

    // --- Data Models (Mirroring Flink Job's data models) ---
    // These classes define the structure of data sent to and received from Kafka.

    [System.Serializable]
    public class GameplayEvent
    {
        public string event_type;
        public long timestamp;
        public string run_id;
        public string player_id;
        public Dictionary<string, object> payload;
    }

    /// <summary>
    /// The top-level wrapper for all messages on the 'adaptive_params' topic.
    /// </summary>
    [System.Serializable]
    public class AdaptiveMessageEnvelope
    {
        public string message_type;
        // The payload is a raw JSON string that must be deserialized separately
        // based on the message_type.
        public string payload;
    }

    /// <summary>
    /// Payload for messages that control the Adaptive Brute's form.
    /// </summary>
    [System.Serializable]
    public class FormAdaptationPayload
    {
        public string playerId;
        public string adaptation_type;
    }

    /// <summary>
    /// Payload for messages that provide a prediction for the Vector Vexer.
    /// </summary>
    [System.Serializable]
    public class VexerPredictionPayload
    {
        public string playerId;
        public PredictionVector predicted_direction;
    }

    [System.Serializable]
    public class PredictionVector
    {
        public float dx;
        public float dy;
    }
}
