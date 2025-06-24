// GameClient/Assets/Scripts/Player/PlayerMovement.cs

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using System; // Required for Action delegate

/// <summary>
/// Handles player movement and the new dash ability based on the Input System.
/// Sends movement events to Kafka and invokes an event on dash.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Base Movement")]
    private float moveSpeed;
    private string playerId;
    private const float BASE_MOVE_SPEED = 5f;

    [Header("Dash Ability")]
    [Tooltip("The high speed applied during the dash.")][SerializeField] public float dashSpeed = 25f;
    [Tooltip("The duration of the dash in seconds.")][SerializeField] public float dashDuration = 0.15f;
    [Tooltip("The cooldown of the dash in seconds.")][SerializeField] public float dashCooldown = 2f;

    [Header("Kafka Settings")]
    [Tooltip("Minimum distance change before sending a new movement event.")]
    [SerializeField] private float positionEventThreshold = 0.1f;
    [Tooltip("Minimum direction change before sending a new movement event.")]
    [SerializeField] private float directionEventThreshold = 0.05f;

    // --- Public Events ---
    /// <summary>
    /// Fired when the player executes a dash. The payload is the direction of the dash.
    /// </summary>
    public static event Action<Vector2> OnPlayerDashed;

    // --- Private State ---
    private KafkaClient kafkaClient;
    private Rigidbody2D rb;
    private PlayerControls playerControls;
    private Vector2 currentMovementInput;
    private Vector2 lastSentPosition;
    private Vector2 lastSentDirection;
    private bool isDashing = false;
    private float lastDashTime = -Mathf.Infinity; // Initialize to allow first dash immediately

    // The Initialize method, called by PlayerInitializer
    public void Initialize(CharacterData data)
    {
        this.playerId = data.characterName;
        this.moveSpeed = BASE_MOVE_SPEED * data.speedMultiplier;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        playerControls = PlayerInputManager.Instance.PlayerControls;
        lastSentPosition = transform.position;
        lastSentDirection = Vector2.zero;

        // Initialize lastDashTime to allow a dash right away
        lastDashTime = -dashCooldown;
    }

    void OnEnable()
    {
        // Subscribe to standard movement actions
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
        // Subscribe to the new Dash action
        playerControls.Player.Dash.performed += OnDashPerformed;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        playerControls.Player.Move.performed -= OnMovePerformed;
        playerControls.Player.Move.canceled -= OnMoveCanceled;
        playerControls.Player.Dash.performed -= OnDashPerformed;
    }

    /// <summary>
    /// Increases the player's movement speed by a flat amount or percentage.
    /// </summary>
    public void IncreaseMoveSpeed(float value, bool isPercentage)
    {
        if (isPercentage)
        {
            moveSpeed *= (1 + value);
        }
        else
        {
            moveSpeed += value;
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentMovementInput = Vector2.zero;
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        // Check if dashing is allowed (not already dashing and not on cooldown)
        if (!isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        lastDashTime = Time.time;

        Vector2 dashDirection = currentMovementInput.normalized;
        if (dashDirection == Vector2.zero)
        {
            dashDirection = Vector2.up;
        }

        // The local event can remain for client-side effects (e.g., sound, particles)
        OnPlayerDashed?.Invoke(dashDirection);

        SendDashEvent(dashDirection);

        // Apply dash force
        rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }

    private void SendDashEvent(Vector2 dashDirection)
    {
        if (kafkaClient == null) return;

        var payload = new Dictionary<string, object>
        {
            { "dash_direction", new Dictionary<string, float> { { "dx", dashDirection.x }, { "dy", dashDirection.y } } }
        };

        kafkaClient.SendGameplayEvent("player_dash_event", playerId, payload);
    }

    void FixedUpdate()
    {
        // Prevent standard movement while dashing
        if (isDashing) return;

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