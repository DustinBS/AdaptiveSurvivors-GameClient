// GameClient/Assets/Scripts/Player/PlayerInteraction.cs

using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles the player's ability to interact with objects in the world.
/// It detects nearby IInteractable objects and triggers their Interact() method
/// when the player presses the interact key.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    // A list to keep track of all interactable objects currently within the trigger range.
    private List<IInteractable> interactablesInRange = new List<IInteractable>();

    private PlayerControls playerControls;

    void Awake()
    {
        playerControls = new PlayerControls();

        // Subscribe to the 'performed' event of the Interact action.
        // This is called once every time the key is pressed down.
        playerControls.Player.Interact.performed += OnInteractPerformed;
    }

    void OnEnable()
    {
        playerControls.Player.Enable();
    }

    void OnDisable()
    {
        playerControls.Player.Disable();
        // It's good practice to unsubscribe from events as well.
        playerControls.Player.Interact.performed -= OnInteractPerformed;
    }

    /// <summary>
    /// This is the callback method that gets executed when the Interact action is performed.
    /// </summary>
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // If there are any interactables in range, interact with the "closest" one.
        // For now, we'll just interact with the first one in the list.
        if (interactablesInRange.Count > 0)
        {
            // A more advanced system could sort by distance or find the one most in front of the player.
            // For our needs, interacting with the first detected object is sufficient.
            interactablesInRange.First().Interact();
        }
    }

    /// <summary>
    /// Called by Unity's physics system when another collider enters this object's trigger collider.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Try to get an IInteractable component from the object that entered the trigger.
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            // If one is found and not already in our list, add it.
            if (!interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Add(interactable);
                Debug.Log($"Entered range of interactable: {other.name}");
            }
        }
    }

    /// <summary>
    /// Called by Unity's physics system when another collider exits this object's trigger collider.
    /// </summary>
    /// <param name="other">The collider that exited the trigger.</param>
    void OnTriggerExit2D(Collider2D other)
    {
        // Try to get an IInteractable component from the object that exited the trigger.
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            // If it's in our list, remove it.
            if (interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Remove(interactable);
                Debug.Log($"Exited range of interactable: {other.name}");
            }
        }
    }
}
