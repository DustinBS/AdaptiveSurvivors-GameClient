// GameClient/Assets/Scripts/Player/PlayerMovement.cs

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // For Dictionary

// This script handles player movement based on the new Input System and sends movement events to Kafka.
// It assumes the player has a Rigidbody2D component for physics-based movement.
// Ensure a KafkaClient GameObject is present in the scene and has the KafkaClient.cs component attached.

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The movement speed of the player.")]
    public float moveSpeed = 5f;

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    // Player ID for Kafka events (could be dynamic later, e.g., fetched from a save file)
    [Tooltip("Unique identifier for this player.")]
    public string playerId = "player_001"; // Assign a unique ID per player instance

    // Rigidbody2D component for physics-based movement
    private Rigidbody2D rb;

    // --- New Input System Variables ---
    private PlayerControls playerControls;
    private Vector2 currentMovementInput; // Store the input vector from the PlayerControls

    // Last sent position for debouncing Kafka events
    private Vector2 lastSentPosition;
    private Vector2 lastSentDirection;
    [Tooltip("Minimum distance change before sending a new movement event to Kafka.")]
    public float positionEventThreshold = 0.1f; // Units in Unity (e.g., meters)
    [Tooltip("Minimum direction change (dot product difference) before sending a new movement event to Kafka.")]
    public float directionEventThreshold = 0.05f; // Small value for noticeable direction change

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement: Rigidbody2D component not found on this GameObject. Please add one.", this);
            enabled = false; // Disable script if Rigidbody2D is missing
        }

        kafkaClient = FindAnyObjectByType<KafkaClient>(); // Using FindAnyObjectByType to resolve deprecation warning
        if (kafkaClient == null)
        {
            Debug.LogError("PlayerMovement: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false; // Disable script if KafkaClient is missing
        }

        // --- New Input System Initialization ---
        playerControls = new PlayerControls();

        // Subscribe to the Move action's performed and canceled events
        playerControls.Player.Move.performed += ctx => currentMovementInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => currentMovementInput = Vector2.zero; // Reset input when action is released

        lastSentPosition = transform.position;
        lastSentDirection = Vector2.zero; // Initialize with no movement
    }

    void OnEnable()
    {
        playerControls.Enable(); // Enable the input action map when the GameObject is enabled
    }

    void OnDisable()
    {
        playerControls.Disable(); // Disable the input action map when the GameObject is disabled
    }

    void FixedUpdate()
    {
        // Get movement input from the new Input System
        Vector2 movement = currentMovementInput.normalized; // Normalize to prevent faster diagonal movement

        // Apply movement to Rigidbody2D
        rb.linearVelocity = movement * moveSpeed;

        // Check if movement event should be sent to Kafka
        SendPlayerMovementEvent(movement);
    }

    /// <summary>
    /// Sends a player_movement_event to Kafka if the position or direction has changed significantly.
    /// </summary>
    /// <param name="currentDirection">The current normalized movement direction.</param>
    private void SendPlayerMovementEvent(Vector2 currentDirection)
    {
        Vector2 currentPosition = transform.position;

        // Check for significant position change
        bool positionChanged = Vector2.Distance(currentPosition, lastSentPosition) >= positionEventThreshold;

        // Check for significant direction change (using dot product for angle comparison)
        bool directionChanged = false;
        if (currentDirection.magnitude > 0.01f && lastSentDirection.magnitude > 0.01f) // Both moving
        {
            float dotProduct = Vector2.Dot(currentDirection.normalized, lastSentDirection.normalized);
            if (Mathf.Abs(1 - dotProduct) > directionEventThreshold) // Check if directions are sufficiently different
            {
                directionChanged = true;
            }
        }
        else if (currentDirection.magnitude > 0.01f != lastSentDirection.magnitude > 0.01f) // One is moving, other is not
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

            // Send event to Kafka
            kafkaClient.SendGameplayEvent("player_movement_event", playerId, payload);

            // Update last sent values
            lastSentPosition = currentPosition;
            lastSentDirection = currentDirection;
        }
    }
}
