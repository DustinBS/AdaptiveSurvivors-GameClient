// GameClient/Assets/Scripts/Enemy/AI/VectorVexerController.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Manages the Vector Vexer. Uses Kafka data for predictions and executes
/// the "Vector Shift" teleport ability.
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public class VectorVexerController : MonoBehaviour
{
    [Header("Ability Settings")]
    [Tooltip("The cooldown in seconds for the Vector Shift ability.")][SerializeField]
    private float abilityCooldown = 5f;
    [Tooltip("How many consecutive wrong predictions before this enemy despawns.")][SerializeField]
    private int wrongPredictionThreshold = 3;
    [Tooltip("How far fodder enemies are teleported during the Vector Shift.")][SerializeField]
    private float teleportDistance = 4f;
    [Tooltip("The radius for checking if two teleported enemies overlap for the 'squish' effect.")][SerializeField]
    private float squishRadius = 0.5f;

    // --- Private State ---
    private EnemyBrain enemyBrain;
    private Camera mainCamera;
    private float lastAbilityTime = -Mathf.Infinity;
    private int consecutiveWrongPredictions = 0;

    // --- Prediction Logic ---
    private Vector2 randomFallbackPrediction;
    private Vector2? lastKafkaPrediction = null;

    private readonly List<Vector2> cardinalDirections = new List<Vector2>
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
        mainCamera = Camera.main; // Cache the main camera reference
    }

    void OnEnable()
    {
        enemyBrain.OnInitialized += HandleBrainInitialized;
        PlayerMovement.OnPlayerDashed += OnPlayerDashed;
        KafkaClient.OnAdaptiveMessageReceived += OnAdaptiveMessageReceived;
    }

    void OnDisable()
    {
        if (enemyBrain != null) { enemyBrain.OnInitialized -= HandleBrainInitialized; }
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

        Vector2 predictionToUse = lastKafkaPrediction.HasValue ? lastKafkaPrediction.Value : randomFallbackPrediction;

        ShowPredictionFeedback(predictionToUse);

        bool isCorrect = IsPredictionCorrect(actualDashDirection, predictionToUse);

        if (isCorrect) { consecutiveWrongPredictions = 0; }
        else { consecutiveWrongPredictions++; }

        // --- TRIGGER TELEPORT COROUTINE ---
        StartCoroutine(VectorShiftRoutine(predictionToUse));

        if (consecutiveWrongPredictions >= wrongPredictionThreshold) { Despawn(); }
        else { MakeNewFallbackPrediction(); }
    }

    /// <summary>
    /// The main coroutine for the Vector Shift ability.
    /// Finds, moves, and then checks enemies for collision.
    /// </summary>
    private IEnumerator VectorShiftRoutine(Vector2 teleportDirection)
    {
        // 1. Find all valid targets (not this Vexer, not other elites/bosses)
        var brainsToTeleport = new List<EnemyBrain>();
        var allEnemies = FindObjectsOfType<EnemyBrain>();
        foreach (var enemy in allEnemies)
        {
            // Exclude ourself and any other potential special enemies
            if (enemy != this.enemyBrain && !enemy.Data.enemyID.Contains("elite"))
            {
                brainsToTeleport.Add(enemy);
            }
        }

        if (brainsToTeleport.Count == 0) yield break;

        // 2. Teleport all enemies simultaneously
        foreach(var brain in brainsToTeleport)
        {
            Vector3 targetPosition = brain.transform.position + (Vector3)teleportDirection * teleportDistance;
            // Use the helper to ensure the position is on screen
            brain.transform.position = ClampPositionToViewport(targetPosition);
        }

        // Wait a single frame to let physics and transforms update
        yield return null;

        // 3. Handle the "Congestion Effect" for any overlapping enemies
        HandleCongestionEffect(brainsToTeleport);
    }

    /// <summary>
    /// Checks for overlaps between teleported enemies and destroys them.
    /// </summary>
    private void HandleCongestionEffect(List<EnemyBrain> teleportedEnemies)
    {
        // Use a HashSet for efficient tracking of which enemies to destroy
        HashSet<EnemyBrain> squishedEnemies = new HashSet<EnemyBrain>();

        for (int i = 0; i < teleportedEnemies.Count; i++)
        {
            for (int j = i + 1; j < teleportedEnemies.Count; j++)
            {
                // Skip checks if one has already been marked for destruction
                if (squishedEnemies.Contains(teleportedEnemies[i]) || squishedEnemies.Contains(teleportedEnemies[j])) continue;

                float distance = Vector3.Distance(teleportedEnemies[i].transform.position, teleportedEnemies[j].transform.position);
                if (distance < squishRadius)
                {
                    squishedEnemies.Add(teleportedEnemies[i]);
                    squishedEnemies.Add(teleportedEnemies[j]);
                }
            }
        }

        // Destroy all marked enemies, awarding XP via the TakeDamage flow
        foreach (var enemy in squishedEnemies)
        {
            if (enemy != null && enemy.Health != null)
            {
                // Reuses existing death logic to grant XP, etc.
                enemy.Health.TakeDamage(enemy.Health.maxHealth, "vexer_squish", false);
            }
        }
    }

    /// <summary>
    /// A helper function to clamp a world position to the camera's viewport.
    /// </summary>
    private Vector3 ClampPositionToViewport(Vector3 worldPosition)
    {
        if (mainCamera == null) return worldPosition;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldPosition);
        // Use a small margin to ensure enemies are fully on-screen
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);

        return mainCamera.ViewportToWorldPoint(viewportPos);
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