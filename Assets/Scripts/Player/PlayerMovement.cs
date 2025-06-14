// GameClient/Assets/Scripts/Player/PlayerMovement.cs

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // For Dictionary

// This script handles player movement based on the new Input System and sends movement events to Kafka.
// It assumes the player has a Rigidbody2D component for physics-based movement.
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The movement speed of the player.")]
    public float moveSpeed = 5f;

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    [Tooltip("Unique identifier for this player.")]
    public string playerId = "player_001"; 

    private Rigidbody2D rb;

    // --- New Input System Variables ---
    private PlayerControls playerControls;
    private Vector2 currentMovementInput;

    // Last sent position for debouncing Kafka events
    private Vector2 lastSentPosition;
    private Vector2 lastSentDirection;
    [Tooltip("Minimum distance change before sending a new movement event to Kafka.")]
    public float positionEventThreshold = 0.1f;
    [Tooltip("Minimum direction change (dot product difference) before sending a new movement event to Kafka.")]
    public float directionEventThreshold = 0.05f; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement: Rigidbody2D component not found on this GameObject. Please add one.", this);
            enabled = false;
        }

        kafkaClient = FindAnyObjectByType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("PlayerMovement: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
        }

        // --- Get PlayerControls from the central manager ---
        playerControls = PlayerInputManager.Instance.PlayerControls;

        lastSentPosition = transform.position;
        lastSentDirection = Vector2.zero;
    }

    void OnEnable()
    {
        // Subscribe to the Move action's performed and canceled events
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        playerControls.Player.Move.performed -= OnMovePerformed;
        playerControls.Player.Move.canceled -= OnMoveCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentMovementInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        Vector2 movement = currentMovementInput.normalized;
        rb.linearVelocity = movement * moveSpeed;
        SendPlayerMovementEvent(movement);
    }

    /// <summary>
    /// Sends a player_movement_event to Kafka if the position or direction has changed significantly.
    /// </summary>
    private void SendPlayerMovementEvent(Vector2 currentDirection)
    {
        if (kafkaClient == null) return;

        Vector2 currentPosition = transform.position;
        bool positionChanged = Vector2.Distance(currentPosition, lastSentPosition) >= positionEventThreshold;
        bool directionChanged = false;

        if (currentDirection.magnitude > 0.01f && lastSentDirection.magnitude > 0.01f)
        {
            float dotProduct = Vector2.Dot(currentDirection.normalized, lastSentDirection.normalized);
            if (Mathf.Abs(1 - dotProduct) > directionEventThreshold)
            {
                directionChanged = true;
            }
        }
        else if (currentDirection.magnitude > 0.01f != lastSentDirection.magnitude > 0.01f)
        {
            directionChanged = true;
        }

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
