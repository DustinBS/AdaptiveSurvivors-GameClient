// GameClient/Assets/Scripts/Player/PlayerInteraction.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Handles player interaction with IInteractable objects. This version is state-aware
/// by checking the active control map in the PlayerInputManager.
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
    private readonly Collider2D[] _colliders = new Collider2D[10];

    private void Awake()
    {
        // This defensive check is crucial for a robust system.
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
        // Attempt to find the new scene's prompt controller. It's okay if it's null.
        _interactionPromptController = FindObjectOfType<InteractionPromptController>();

        // Always reset state on scene load to prevent prompts from sticking.
        _closestInteractable = null;
        _interactionPromptController?.HidePrompt();
    }

    private void Update()
    {
        // The core of the fix: check the input map directly.
        // If player controls are not enabled, we should not be able to interact.
        if (!PlayerInputManager.Instance.IsPlayerControlsEnabled)
        {
            // If we switch to UI mode while a prompt is visible, hide it.
            if (_closestInteractable != null)
            {
                _interactionPromptController?.HidePrompt();
                _closestInteractable = null;
            }
            return;
        }

        // If we get here, player controls are active. Proceed with finding interactables.
        FindAndHandleClosestInteractable();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // The Update loop's guard prevents this from being called at the wrong time,
        // but an extra check here is good defensive practice.
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

            if (_closestInteractable == null)
            {
                _interactionPromptController.HidePrompt();
            }
            else
            {
                _interactionPromptController.ShowPrompt(_closestInteractable.GetInteractionPrompt(), newClosestTransform);
            }
        }
    }
}
