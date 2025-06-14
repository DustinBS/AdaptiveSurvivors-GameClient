// GameClient/Assets/Scripts/Player/PlayerInteraction.cs
using UnityEngine;
using System.Linq; // Required for OrderBy

[RequireComponent(typeof(InteractionPromptController))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The point from which the interaction check is cast.")]
    [SerializeField] private Transform interactionPoint;
    [Tooltip("The radius of the interaction check circle.")]
    [SerializeField] private float interactionRadius = 1.5f;
    [Tooltip("The layer(s) containing interactable objects.")]
    [SerializeField] private LayerMask interactableLayer;

    // --- Private State ---
    private InteractionPromptController interactionPromptController;
    // The single interactable object that is closest to the player and in range.
    private IInteractable closestInteractable;

    private void Awake()
    {
        // Get a reference to the prompt controller on the same GameObject.
        interactionPromptController = GetComponent<InteractionPromptController>();
    }

    private void OnEnable()
    {
        PlayerInputManager.OnInteract += HandleInteract;
    }

    private void OnDisable()
    {
        PlayerInputManager.OnInteract -= HandleInteract;
    }

    private void Update()
    {
        // This loop runs every frame to check for the nearest interactable object.
        FindAndDisplayClosestInteractable();
    }

    private void FindAndDisplayClosestInteractable()
    {
        // Find all colliders within the interaction radius on the specified layer.
        var colliders = Physics2D.OverlapCircleAll(interactionPoint.position, interactionRadius, interactableLayer);

        IInteractable newClosest = null;
        float closestDistance = float.MaxValue;

        if (colliders.Length > 0)
        {
            // Iterate through all found colliders to find the closest valid one.
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<IInteractable>(out var interactable))
                {
                    float distance = Vector2.Distance(interactionPoint.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        newClosest = interactable;
                    }
                }
            }
        }

        // Check if the closest interactable has changed since the last frame.
        if (newClosest != closestInteractable)
        {
            // If a new interactable is found, show its prompt.
            if (newClosest != null)
            {
                // We need to get the transform from the component to position the UI.
                var targetTransform = (newClosest as MonoBehaviour)?.transform;
                if (targetTransform != null)
                {
                    interactionPromptController.ShowPrompt(targetTransform, newClosest.GetInteractionPrompt());
                }
            }
            else
            {
                // If there's no new interactable (i.e., we've moved out of range), hide the prompt.
                interactionPromptController.HidePrompt();
            }

            closestInteractable = newClosest;
        }
    }

    /// <summary>
    /// Called by the PlayerInputManager. Executes the interaction on the closest object.
    /// </summary>
    private void HandleInteract()
    {
        // If there is a valid closest interactable, call its Interact method.
        if (closestInteractable != null)
        {
            closestInteractable.Interact();
        }
    }

    private void OnDrawGizmos()
    {
        if (interactionPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint.position, interactionRadius);
    }
}
