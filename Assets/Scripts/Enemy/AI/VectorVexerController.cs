// GameClient/Assets/Scripts/Enemy/AI/VectorVexerController.cs

using UnityEngine;
using System.Collections.Generic; // Required for List
using Newtonsoft.Json;

/// <summary>
/// Manages the unique behavior of the Vector Vexer elite enemy.
/// Listens for player dashes and triggers its "Vector Shift" ability.
/// random prediction as an offline fallback
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public class VectorVexerController : MonoBehaviour
{
    [Header("Ability Settings")]
    [Tooltip("The cooldown in seconds for the Vector Shift ability.")][SerializeField]
    private float abilityCooldown = 5f;
    [Tooltip("How many consecutive wrong predictions before this enemy despawns.")][SerializeField]
    private int wrongPredictionThreshold = 3;

    // --- Private State ---
    private EnemyBrain enemyBrain;
    private float lastAbilityTime = -Mathf.Infinity;
    private int consecutiveWrongPredictions = 0;

    // --- Prediction Logic ---
    // Used for the OFFLINE FALLBACK.
    private Vector2 randomFallbackPrediction;
    // Used for ONLINE mode. Nullable so we know if we've ever received a message.
    private Vector2? lastKafkaPrediction = null;

    private readonly List<Vector2> cardinalDirections = new List<Vector2>
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
    }

    void OnEnable()
    {
        enemyBrain.OnInitialized += HandleBrainInitialized;
        PlayerMovement.OnPlayerDashed += OnPlayerDashed;
        KafkaClient.OnAdaptiveMessageReceived += OnAdaptiveMessageReceived;
    }

    void OnDisable()
    {
        if (enemyBrain != null)
        {
            enemyBrain.OnInitialized -= HandleBrainInitialized;
        }
        PlayerMovement.OnPlayerDashed -= OnPlayerDashed;
        KafkaClient.OnAdaptiveMessageReceived -= OnAdaptiveMessageReceived;
    }

    /// <summary>
    /// Handler for Kafka messages. Filters for Vexer-specific prediction updates.
    /// </summary>
    private void OnAdaptiveMessageReceived(KafkaClient.AdaptiveMessageEnvelope envelope)
    {
        if (envelope.message_type != "vexer_prediction_update") return;

        try
        {
            var payload = JsonConvert.DeserializeObject<KafkaClient.VexerPredictionPayload>(envelope.payload);
            lastKafkaPrediction = new Vector2(payload.predicted_direction.dx, payload.predicted_direction.dy);
            Debug.Log($"Vexer received new prediction from Kafka: {lastKafkaPrediction.Value}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to deserialize VexerPredictionPayload: {e.Message}\nPayload: {envelope.payload}");
        }
    }

    /// <summary>
    /// Safe entry point, called after the EnemyBrain is fully initialized.
    /// </summary>
    private void HandleBrainInitialized()
    {
        lastAbilityTime = -abilityCooldown;
        MakeNewFallbackPrediction();
    }

    /// <summary>
    /// The core logic trigger, called whenever the player dashes.
    /// </summary>
    private void OnPlayerDashed(Vector2 actualDashDirection)
    {
        if (Time.time < lastAbilityTime + abilityCooldown) return;
        lastAbilityTime = Time.time;

        // --- OFFLINE/ONLINE LOGIC ---
        // Decide which prediction to use based on whether we've received data from Kafka.
        Vector2 predictionToUse;
        if (lastKafkaPrediction.HasValue)
        {
            predictionToUse = lastKafkaPrediction.Value; // ONLINE: Use Kafka data
        }
        else
        {
            predictionToUse = randomFallbackPrediction; // OFFLINE: Use random fallback
        }

        ShowPredictionFeedback(predictionToUse);

        bool isCorrect = IsPredictionCorrect(actualDashDirection, predictionToUse);

        if (isCorrect) { consecutiveWrongPredictions = 0; }
        else { consecutiveWrongPredictions++; }

        Debug.Log($"Vexer used Vector Shift. Predicted: {predictionToUse}, Correct: {isCorrect}");

        if (consecutiveWrongPredictions >= wrongPredictionThreshold)
        {
            Despawn();
        }
        else
        {
            // Always generate a new random prediction in case Kafka connection is lost.
            MakeNewFallbackPrediction();
        }
    }

    /// <summary>
    /// Generates a new random prediction for the offline fallback.
    /// </summary>
    private void MakeNewFallbackPrediction()
    {
        int randomIndex = Random.Range(0, cardinalDirections.Count);
        randomFallbackPrediction = cardinalDirections[randomIndex];
    }

    /// <summary>
    /// Compares the Vexer's orthogonal prediction to the player's (potentially diagonal) dash.
    /// </summary>
    private bool IsPredictionCorrect(Vector2 playerDashDirection, Vector2 predictedDirection)
    {
        // Find the dominant axis of the player's dash to compare against our cardinal prediction.
        Vector2 effectiveDirection;
        if (Mathf.Abs(playerDashDirection.x) > Mathf.Abs(playerDashDirection.y))
        {
            effectiveDirection = new Vector2(Mathf.Sign(playerDashDirection.x), 0); // Horizontal dash
        }
        else
        {
            effectiveDirection = new Vector2(0, Mathf.Sign(playerDashDirection.y)); // Vertical dash
        }

        return effectiveDirection == predictedDirection;
    }

    /// <summary>
    /// Displays the predicted direction as text above the Vexer's head.
    /// </summary>
    private void ShowPredictionFeedback(Vector2 directionToShow)
    {
        if (ContextualFeedbackManager.Instance == null) return;

        string arrow = "?";
        if (directionToShow == Vector2.up) arrow = "↑";
        if (directionToShow == Vector2.down) arrow = "↓";
        if (directionToShow == Vector2.left) arrow = "←";
        if (directionToShow == Vector2.right) arrow = "→";

        ContextualFeedbackManager.Instance.ShowFeedback(arrow, transform.position);
    }

    /// <summary>
    /// Handles the despawning of the Vexer.
    /// </summary>
    private void Despawn()
    {
        Debug.Log("Vexer failed 3 predictions and despawned.");
        // TODO: Trigger a "puff of smoke" particle effect.
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.OnUniqueEnemyDefeated(enemyBrain.Data);
        }
        Destroy(gameObject);
    }
}