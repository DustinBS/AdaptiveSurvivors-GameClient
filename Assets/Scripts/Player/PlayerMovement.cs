// GameClient/Assets/Scripts/Player/PlayerMovement.cs

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Handles player movement and the dash ability.
/// Sends movement and dash events to Kafka and invokes a C# event on dash.
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
    [Tooltip("Minimum distance change before sending a new movement event.")][SerializeField] 
    private float positionEventThreshold = 0.1f;
    [Tooltip("Minimum direction change before sending a new movement event.")][SerializeField] 
    private float directionEventThreshold = 0.05f;

    // --- Public Events ---
    /// <summary>
    /// Fired when the player executes a dash. The payload is the direction of the dash.
    /// (Ensured to be declared only once).
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
    private float lastDashTime = -Mathf.Infinity;

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
        lastDashTime = -dashCooldown;
    }

    void OnEnable()
    {
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
        playerControls.Player.Dash.performed += OnDashPerformed;
    }

    void OnDisable()
    {
        playerControls.Player.Move.performed -= OnMovePerformed;
        playerControls.Player.Move.canceled -= OnMoveCanceled;
        playerControls.Player.Dash.performed -= OnDashPerformed;
    }

    public void ModifyMoveSpeed(float value, bool isPercentage)
    {
        if (isPercentage) { moveSpeed *= (1 + value); }
        else { moveSpeed += value; }
    }

    public void ModifyDashCooldown(float value, bool isPercentage)
    {
        if (isPercentage) { dashCooldown *= (1 + value); }
        else { dashCooldown += value; }
        // Ensure cooldown doesn't go below a minimum threshold.
        if (dashCooldown < 0.1f) dashCooldown = 0.1f;
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
        
        // Invoke local C# event
        OnPlayerDashed?.Invoke(dashDirection);

        // Send Kafka event
        SendPlayerDashEvent(dashDirection);

        rb.linearVelocity = dashDirection * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        rb.linearVelocity = Vector2.zero;
        
        isDashing = false;
    }

    private void SendPlayerDashEvent(Vector2 dashDirection)
    {
        if (kafkaClient == null) return;
        var payload = new Dictionary<string, object>
        {
            { "direction", new Dictionary<string, float> { { "dx", dashDirection.x }, { "dy", dashDirection.y } } }
        };
        kafkaClient.SendGameplayEvent("player_dash_event", playerId, payload);
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        Vector2 movement = currentMovementInput.normalized;
        rb.linearVelocity = movement * moveSpeed;
        SendPlayerMovementEvent(movement);
    }

    private void SendPlayerMovementEvent(Vector2 currentDirection)
    {
        if (kafkaClient == null) return;

        Vector2 currentPosition = transform.position;
        bool positionChanged = Vector2.Distance(currentPosition, lastSentPosition) >= positionEventThreshold;
        bool directionChanged = false;

        if (currentDirection.magnitude > 0.01f && lastSentDirection.magnitude > 0.01f)
        {
            if (Mathf.Abs(1 - Vector2.Dot(currentDirection.normalized, lastSentDirection.normalized)) > directionEventThreshold)
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