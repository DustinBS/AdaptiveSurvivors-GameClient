// GameClient/Assets/Scripts/Environment/SceneChangeInteractable.cs
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A reusable component that makes any object an interactable trigger
/// for loading a different scene. Implements IInteractable to hook into
/// the player's interaction system.
/// </summary>
public class SceneChangeInteractable : MonoBehaviour, IInteractable
{
    [Header("Scene Configuration")]
    [Tooltip("The name of the scene to load when interacted with.")]
    [SerializeField] private string sceneToLoad;

    [Header("UI Prompt")]
    [Tooltip("The text to display in the interaction prompt (e.g., 'Enter Portal').")]
    [SerializeField] private string interactionPrompt = "Interact";

    private void Start()
    {
        // Defensive check to ensure a scene has been specified by the designer.
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning($"SceneChangeInteractable on '{gameObject.name}' has no scene specified in the inspector.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Executes the interaction: loads the configured scene using the static SceneLoader.
    /// This method is called by PlayerInteraction.
    /// </summary>
    public void Interact()
    {
        // Call the static method directly on the SceneLoader class.
        SceneLoader.LoadScene(sceneToLoad);
    }

    /// <summary>
    /// Provides the UI text for the interaction prompt.
    /// This method is called by PlayerInteraction.
    /// </summary>
    /// <returns>The configured prompt text.</returns>
    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Door;
    }

}
