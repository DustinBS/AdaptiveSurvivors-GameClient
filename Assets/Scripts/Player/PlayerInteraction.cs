// GameClient/Assets/Scripts/Player/PlayerInteraction.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages player interaction with IInteractable objects.
/// Handles scene changes and ensures jitter-free UI updates.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("The radius to check for interactable objects.")]
    [SerializeField] private float interactionRadius = 1.5f;

    private PlayerControls playerControls;
    private InteractionPromptController _interactionPromptController;

    private IInteractable _closestInteractable;
    private Transform _closestInteractableTransform; // Cached for UI positioning

    private void Awake()
    {
        if (PlayerInputManager.Instance == null)
        {
            Debug.LogError("PlayerInteraction requires PlayerInputManager.Instance.", this);
            enabled = false;
            return;
        }
        playerControls = PlayerInputManager.Instance.PlayerControls;
        _interactionPromptController = FindFirstObjectByType<InteractionPromptController>();
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
        _interactionPromptController = FindFirstObjectByType<InteractionPromptController>();
        _closestInteractable = null;
        _closestInteractableTransform = null;
        _interactionPromptController?.HidePrompt();
    }

    private void Update()
    {
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
    /// Updates UI position in LateUpdate for smooth, jitter-free movement.
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
        if (_interactionPromptController == null)
        {
            if (_closestInteractable != null)
            {
                _closestInteractable = null;
                _closestInteractableTransform = null;
            }
            return;
        }

        Collider2D[] foundColliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);

        IInteractable newClosestInteractable = null;
        Transform newClosestTransform = null;
        float closestDistanceSqr = float.MaxValue;

        if (foundColliders != null && foundColliders.Length > 0)
        {
            foreach (var collider in foundColliders)
            {
                var interactable = collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    float dSqrToTarget = (transform.position - collider.transform.position).sqrMagnitude;
                    if (dSqrToTarget < closestDistanceSqr)
                    {
                        closestDistanceSqr = dSqrToTarget;
                        newClosestInteractable = interactable;
                        newClosestTransform = collider.transform;
                    }
                }
            }
        }

        if (newClosestInteractable != _closestInteractable)
        {
            _closestInteractable = newClosestInteractable;
            _closestInteractableTransform = newClosestTransform;

            if (_closestInteractable == null)
            {
                _interactionPromptController.HidePrompt();
            }
            else
            {
                _interactionPromptController.ShowPrompt(_closestInteractable.GetInteractionPrompt());
            }
        }
    }
}