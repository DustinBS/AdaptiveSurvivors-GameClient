// GameClient/Assets/Scripts/Environment/SceneChangeInteractable.cs
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A reusable component for any object that should load a new scene upon interaction.
/// Implements the IInteractable interface.
/// </summary>
public class SceneChangeInteractable : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [Tooltip("The exact name of the scene to load when interacted with.")]
    [SerializeField] private string sceneToLoad;

    [Tooltip("The text that will be displayed in the interaction prompt UI (e.g., 'Enter Battle Portal').")]
    [SerializeField] private string interactionPromptText;

    public void Interact()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene to load is not specified on " + gameObject.name, this);
            return;
        }

        Debug.Log($"Interacted with {gameObject.name}, loading scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    public string GetInteractionPrompt()
    {
        // Return the custom text defined in the inspector.
        // If no text is provided, return the name of the object as a fallback.
        return string.IsNullOrEmpty(interactionPromptText) ? gameObject.name : interactionPromptText;
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.SceneChange;
    }
}
