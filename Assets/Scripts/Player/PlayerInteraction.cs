// GameClient/Assets/Scripts/Player/PlayerInteraction.cs

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles the player's ability to interact with objects in the world.
/// Detects nearby IInteractable objects and triggers their Interact() method
/// when the player presses the interact key.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    private List<IInteractable> interactablesInRange = new List<IInteractable>();
    private PlayerControls playerControls;

    void Awake()
    {
        playerControls = new PlayerControls();
    }

    void OnEnable()
    {
        // It's robust to enable the controls and subscribe to events in OnEnable.
        playerControls.Player.Enable();
        playerControls.Player.Interact.performed += OnInteractPerformed;
    }

    void OnDisable()
    {
        // ALWAYS unsubscribe and disable in OnDisable to prevent errors.
        playerControls.Player.Interact.performed -= OnInteractPerformed;
        playerControls.Player.Disable();
    }

    /// <summary>
    /// Callback method for when the Interact input action is performed.
    /// </summary>
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Debug.Log("Interact action performed by player.");
        if (interactablesInRange.Count > 0)
        {
            // Interact with the first object in range.
            interactablesInRange.First().Interact();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
            // Debug.Log($"Entered range of interactable: {other.name}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Remove(interactable);
            // Debug.Log($"Exited range of interactable: {other.name}");
        }
    }
}
