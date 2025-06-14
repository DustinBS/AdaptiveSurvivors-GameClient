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
        // Get the single instance of PlayerControls from the manager
        playerControls = PlayerInputManager.Instance.PlayerControls;
    }

    void OnEnable()
    {
        playerControls.Player.Interact.performed += OnInteractPerformed;
    }

    void OnDisable()
    {
        playerControls.Player.Interact.performed -= OnInteractPerformed;
    }

    /// <summary>
    /// Callback method for when the Interact input action is performed.
    /// </summary>
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (interactablesInRange.Count > 0)
        {
            // Interact with the first object in range.
            // Using First() is simple, but for more complex scenarios you might want
            // to find the closest interactable object.
            interactablesInRange.First().Interact();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Remove(interactable);
        }
    }
}
