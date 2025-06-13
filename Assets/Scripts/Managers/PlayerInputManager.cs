// GameClient/Assets/Scripts/Managers/PlayerInputManager.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    // --- Public Events ---
    // Other scripts will subscribe to these events to receive input.
    public static event Action<Vector2> OnMove;
    public static event Action OnInteract;
    public static event Action OnAttack;

    // --- State Management ---
    private PlayerControls playerControls;
    private bool isInputEnabled = true;

    private void Awake()
    {
        // Initialize the Input System class
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        // Enable the entire action map
        playerControls.Player.Enable();

        // Subscribe our handler methods to the events from the Input System
        playerControls.Player.Move.performed += OnMoveInput;
        playerControls.Player.Move.canceled += OnMoveInput;
        playerControls.Player.Interact.performed += OnInteractInput;
        playerControls.Player.Attack.performed += OnAttackInput;

        // Subscribe to the dialogue manager to disable/enable input
        DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;
    }

    private void OnDisable()
    {
        // Disable the action map
        playerControls.Player.Disable();

        // Unsubscribe from all events to prevent errors
        playerControls.Player.Move.performed -= OnMoveInput;
        playerControls.Player.Move.canceled -= OnMoveInput;
        playerControls.Player.Interact.performed -= OnInteractInput;
        playerControls.Player.Attack.performed -= OnAttackInput;

        DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;
    }

    /// <summary>
    /// Called by the Input System when the Move action is performed or canceled.
    /// </summary>
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        if (!isInputEnabled)
        {
            // If input is disabled, broadcast a zero vector to stop movement.
            OnMove?.Invoke(Vector2.zero);
            return;
        }
        OnMove?.Invoke(context.ReadValue<Vector2>());
    }

    /// <summary>
    /// Called by the Input System when the Interact action is performed.
    /// </summary>
    private void OnInteractInput(InputAction.CallbackContext context)
    {
        if (!isInputEnabled) return;
        OnInteract?.Invoke();
    }

    /// <summary>
    /// Called by the Input System when the Attack action is performed.
    /// </summary>
    private void OnAttackInput(InputAction.CallbackContext context)
    {
        if (!isInputEnabled) return;
        OnAttack?.Invoke();
    }

    /// <summary>
    /// Handles the event fired by the DialogueManager to enable/disable input.
    /// </summary>
    private void HandleDialogueStateChanged(bool isDialogueActive)
    {
        isInputEnabled = !isDialogueActive;

        // If dialogue starts, we ensure the character stops moving.
        if (isDialogueActive)
        {
            OnMove?.Invoke(Vector2.zero);
        }
    }
}
