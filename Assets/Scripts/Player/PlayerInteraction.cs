// GameClient/Assets/Scripts/Player/PlayerInteraction.cs
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The point from which the interaction check is cast.")]
    [SerializeField] private Transform interactionPoint;
    [Tooltip("The radius of the interaction check circle.")]
    [SerializeField] private float interactionRadius = 0.5f;
    [Tooltip("The layer(s) containing interactable objects.")]
    [SerializeField] private LayerMask interactableLayer;

    private void OnEnable()
    {
        // Subscribe to the central input manager's interact event
        PlayerInputManager.OnInteract += HandleInteract;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors
        PlayerInputManager.OnInteract -= HandleInteract;
    }

    /// <summary>
    /// Called when the PlayerInputManager broadcasts an interact event.
    /// </summary>
    private void HandleInteract()
    {
        // Check for interactable objects within the defined radius
        var collider = Physics2D.OverlapCircle(interactionPoint.position, interactionRadius, interactableLayer);

        if (collider != null)
        {
            // Try to get the IInteractable component and trigger the interaction
            if (collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact();
            }
        }
    }

    // Optional: Draw a visual gizmo in the editor to see the interaction radius
    private void OnDrawGizmos()
    {
        if (interactionPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint.position, interactionRadius);
    }
}
