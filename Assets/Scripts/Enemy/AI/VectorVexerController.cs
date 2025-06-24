// GameClient/Assets/Scripts/Enemy/AI/VectorVexerController.cs

using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// Manages the unique behavior of the Vector Vexer elite enemy.
/// Listens for player dashes and triggers its "Vector Shift" ability.
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
    private Vector2 predictedDirection;

    // A list of possible orthogonal directions for predictions.
    private readonly List<Vector2> cardinalDirections = new List<Vector2>
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
    }

    void OnEnable()
    {
        // Subscribe to events in a controlled manner.
        enemyBrain.OnInitialized += HandleBrainInitialized;
        PlayerMovement.OnPlayerDashed += OnPlayerDashed;
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks.
        if (enemyBrain != null)
        {
            enemyBrain.OnInitialized -= HandleBrainInitialized;
        }
        PlayerMovement.OnPlayerDashed -= OnPlayerDashed;
    }

    /// <summary>
    /// Safe entry point, called after the EnemyBrain is fully initialized.
    /// </summary>
    private void HandleBrainInitialized()
    {
        lastAbilityTime = -abilityCooldown; // Allow first ability use immediately.
        MakeNewPrediction();
    }

    /// <summary>
    /// The core logic trigger, called whenever the player dashes.
    /// </summary>
    private void OnPlayerDashed(Vector2 actualDashDirection)
    {
        // Check if the ability is off cooldown.
        if (Time.time < lastAbilityTime + abilityCooldown) return;

        lastAbilityTime = Time.time;

        // Reveal the prediction to the player using the feedback system.
        ShowPredictionFeedback();

        // Compare the prediction to the actual dash.
        bool isCorrect = IsPredictionCorrect(actualDashDirection);

        if (isCorrect)
        {
            consecutiveWrongPredictions = 0;
            // TODO: Play "correct prediction" sound/visuals
        }
        else
        {
            consecutiveWrongPredictions++;
            // TODO: Play "wrong prediction" sound/visuals
        }

        // --- TRIGGER TELEPORT LOGIC HERE ---
        // We will implement this in the next phase. For now, we can log it.
        Debug.Log($"Vexer used Vector Shift. Predicted: {predictedDirection}, Correct: {isCorrect}");

        // Check for despawn condition.
        if (consecutiveWrongPredictions >= wrongPredictionThreshold)
        {
            Despawn();
        }
        else
        {
            // Prepare for the next cycle.
            MakeNewPrediction();
        }
    }

    /// <summary>
    /// Selects a new random cardinal direction for the next prediction.
    /// </summary>
    private void MakeNewPrediction()
    {
        int randomIndex = Random.Range(0, cardinalDirections.Count);
        predictedDirection = cardinalDirections[randomIndex];
        // TODO: Update a visual indicator on the Vexer to show it's "ready".
    }

    /// <summary>
    /// Compares the Vexer's orthogonal prediction to the player's (potentially diagonal) dash.
    /// </summary>
    private bool IsPredictionCorrect(Vector2 playerDashDirection)
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
    private void ShowPredictionFeedback()
    {
        if (ContextualFeedbackManager.Instance == null) return;

        string arrow = "?";
        if (predictedDirection == Vector2.up) arrow = "↑";
        if (predictedDirection == Vector2.down) arrow = "↓";
        if (predictedDirection == Vector2.left) arrow = "←";
        if (predictedDirection == Vector2.right) arrow = "→";

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