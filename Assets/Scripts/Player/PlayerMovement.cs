// GameClient/Assets/Scripts/Player/PlayerMovement.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The current movement speed of the player.")]
    public float moveSpeed = 5f; // [FIX] Made public to be accessible by other scripts like PlayerExperience

    [Header("Kafka Dependencies")]
    [Tooltip("Unique identifier for this player.")]
    public string playerId = "player_001";
    [Tooltip("Minimum distance change before sending a new movement event to Kafka.")]
    public float positionEventThreshold = 0.1f;
    [Tooltip("Minimum direction change before sending a new movement event to Kafka.")]
    public float directionEventThreshold = 0.05f;

    // --- Private State ---
    private Rigidbody2D rb;
    private KafkaClient kafkaClient;
    private Vector2 moveInput;
    private Vector2 lastSentPosition;
    private Vector2 lastSentDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        kafkaClient = FindObjectOfType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogWarning("PlayerMovement: KafkaClient not found in the scene. Kafka events will not be sent.", this);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the central input manager's move event
        PlayerInputManager.OnMove += HandleMoveInput;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors
        PlayerInputManager.OnMove -= HandleMoveInput;
    }

    /// <summary>
    /// Receives move input from the PlayerInputManager and stores it.
    /// </summary>
    private void HandleMoveInput(Vector2 newMoveInput)
    {
        moveInput = newMoveInput;
    }

    private void FixedUpdate()
    {
        // Apply physics-based movement
        if (moveInput != Vector2.zero)
        {
            // [FIX] We need to apply the Character's speed multiplier
            float finalSpeed = moveSpeed * (GetComponent<PlayerStatus>()?.playerData.characterData.speedMultiplier ?? 1f);
            rb.linearVelocity = moveInput.normalized * finalSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Check if movement event should be sent to Kafka
        SendPlayerMovementEvent(moveInput.normalized);
    }

    private void SendPlayerMovementEvent(Vector2 currentDirection)
    {
        if (kafkaClient == null) return;

        Vector2 currentPosition = transform.position;

        bool positionChanged = Vector2.Distance(currentPosition, lastSentPosition) >= positionEventThreshold;
        bool directionChanged = Vector2.Dot(currentDirection, lastSentDirection) < (1.0f - directionEventThreshold);

        if (positionChanged || directionChanged)
        {
            var payload = new Dictionary<string, object>
            {
                { "pos", new Dictionary<string, float> { { "x", currentPosition.x }, { "y", currentPosition.y } } },
                { "dir", new Dictionary<string, float> { { "dx", currentDirection.x }, { "dy", currentDirection.y } } }
            };

            kafkaClient.SendGameplayEvent("player_movement_event", playerId, payload);
            lastSentPosition = currentPosition;
            lastSentDirection = currentDirection;
        }
    }
}
