// GameClient/Assets/Scripts/UI/DeathMenuController.cs

using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// This script controls the logic for the DeathMenu UI Document.
/// It finds the buttons defined in the UXML and registers callbacks for their click events.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DeathMenuController : MonoBehaviour
{
    private Button restartButton;
    private Button hubButton;

    void OnEnable()
    {
        // It's best practice to get references and register callbacks in OnEnable.
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        // Query the UXML document to find the buttons by their names.
        restartButton = root.Q<Button>("RestartButton");
        hubButton = root.Q<Button>("HubButton");

        // Check if the buttons were found before trying to register callbacks.
        if (restartButton != null)
        {
            // Register a callback for the button's 'clicked' event.
            // This links the button click to our scene loading logic.
            restartButton.clicked += OnRestartButtonClicked;
        }

        if (hubButton != null)
        {
            hubButton.clicked += OnHubButtonClicked;
        }
    }

    void OnDisable()
    {
        // ALWAYS unregister callbacks in OnDisable to prevent errors when the object is destroyed
        // or when the UI is reloaded.
        if (restartButton != null)
        {
            restartButton.clicked -= OnRestartButtonClicked;
        }

        if (hubButton != null)
        {
            hubButton.clicked -= OnHubButtonClicked;
        }
    }

    private void OnRestartButtonClicked()
    {
        Debug.Log("Restart button clicked. Loading Battle scene...");
        // Call the static method from our SceneLoader.
        SceneLoader.LoadBattleScene();
    }

    private void OnHubButtonClicked()
    {
        Debug.Log("Return to Hub button clicked. Loading Hub scene...");
        // Call the static method from our SceneLoader.
        SceneLoader.LoadHubScene();
    }
}
