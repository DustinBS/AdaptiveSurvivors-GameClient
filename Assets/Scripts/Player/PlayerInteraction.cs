// GameClient/Assets/Scripts/Player/PlayerInteraction.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Handles player interaction with IInteractable objects. This version is state-aware,
/// robust against scene changes, and uses LateUpdate for jitter-free UI positioning.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("The radius around the player to check for interactable objects.")]
    [SerializeField] private float interactionRadius = 1.5f;

    // --- References ---
    private PlayerControls playerControls;
    private InteractionPromptController _interactionPromptController;

    // --- State ---
    private IInteractable _closestInteractable;
    private Transform _closestInteractableTransform; // Cached transform for LateUpdate
    private readonly Collider2D[] _colliders = new Collider2D[10];

    private void Awake()
    {
        if (PlayerInputManager.Instance == null)
        {
            Debug.LogError("PlayerInteraction: PlayerInputManager.Instance is null. This script cannot function.", this);
            enabled = false;
            return;
        }
        playerControls = PlayerInputManager.Instance.PlayerControls;
    }

    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Interact.performed += OnInteractPerformed;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Interact.performed -= OnInteractPerformed;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _interactionPromptController = FindObjectOfType<InteractionPromptController>();

        // Reset state on scene load to prevent prompts from sticking.
        _closestInteractable = null;
        _closestInteractableTransform = null;
        _interactionPromptController?.HidePrompt();
    }

    private void Update()
    {
        // If player controls are not enabled, hide the prompt and do nothing.
        if (!PlayerInputManager.Instance.IsPlayerControlsEnabled)
        {
            if (_closestInteractable != null)
            {
                _interactionPromptController?.HidePrompt();
                _closestInteractable = null;
                _closestInteractableTransform = null;
            }
            return;
        }

        FindAndHandleClosestInteractable();
    }

    /// <summary>
    /// Jitter-Fix: The UI position is updated in LateUpdate, which runs *after* all
    /// game logic and animation has finished for the frame. This ensures the UI
    /// is positioned based on the object's final position for that frame.
    /// </summary>
    private void LateUpdate()
    {
        if (_closestInteractable != null && _interactionPromptController != null)
        {
            _interactionPromptController.UpdatePosition(_closestInteractableTransform);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (_closestInteractable != null && PlayerInputManager.Instance.IsPlayerControlsEnabled)
        {
            _closestInteractable.Interact();
        }
    }

    private void FindAndHandleClosestInteractable()
    {
        if (_interactionPromptController == null) return;

        int numFound = Physics2D.OverlapCircleNonAlloc(transform.position, interactionRadius, _colliders);

        IInteractable newClosestInteractable = null;
        Transform newClosestTransform = null;
        float closestDistanceSqr = float.MaxValue;

        if (numFound > 0)
        {
            for (int i = 0; i < numFound; i++)
            {
                var interactable = _colliders[i].GetComponent<IInteractable>();
                if (interactable != null)
                {
                    float dSqrToTarget = (transform.position - _colliders[i].transform.position).sqrMagnitude;
                    if (dSqrToTarget < closestDistanceSqr)
                    {
                        closestDistanceSqr = dSqrToTarget;
                        newClosestInteractable = interactable;
                        newClosestTransform = _colliders[i].transform;
                    }
                }
            }
        }

        if (newClosestInteractable != _closestInteractable)
        {
            _closestInteractable = newClosestInteractable;
            // Cache the transform of the new closest interactable.
            _closestInteractableTransform = newClosestTransform;

            if (_closestInteractable == null)
            {
                _interactionPromptController.HidePrompt();
            }
            else
            {
                // We no longer pass the transform here; it's handled by LateUpdate.
                _interactionPromptController.ShowPrompt(_closestInteractable.GetInteractionPrompt());
            }
        }
    }
}
