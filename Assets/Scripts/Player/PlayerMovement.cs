// GameClient/Assets/Scripts/Player/PlayerMovement.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // Subscribe to the new central input manager's move event
        PlayerInputManager.OnMove += HandleMove;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors
        PlayerInputManager.OnMove -= HandleMove;
    }

    /// <summary>
    /// Receives move input from the PlayerInputManager.
    /// </summary>
    /// <param name="newMoveInput">The Vector2 direction from the input manager.</param>
    private void HandleMove(Vector2 newMoveInput)
    {
        moveInput = newMoveInput;
    }

    private void FixedUpdate()
    {
        // Physics-based movement logic remains the same
        if (moveInput != Vector2.zero)
        {
            rb.MovePosition(rb.position + moveInput.normalized * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // This ensures the player stops when there's no input
            rb.velocity = Vector2.zero;
        }
    }
}
