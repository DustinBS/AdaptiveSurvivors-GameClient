// GameClient/Assets/Scripts/UI/InteractionPromptController.cs
using UnityEngine;
using UnityEngine.UIElements;

public class InteractionPromptController : MonoBehaviour
{
    [Header("UI Asset")]
    [Tooltip("The VisualTreeAsset for the interaction prompt UI.")]
    [SerializeField] private VisualTreeAsset interactionPromptAsset;

    private VisualElement promptContainer;
    private Label actionLabel;
    private Camera mainCamera;

    private Transform currentTarget; // The transform of the interactable object

    private void Start()
    {
        // The prompt should exist within the main UI Document of the scene.
        // This assumes you have a UIDocument component in your scene's hierarchy.
        var sceneUIDocument = FindObjectOfType<UIDocument>();
        if (sceneUIDocument == null)
        {
            Debug.LogError("No UIDocument found in the scene. The InteractionPrompt cannot be created.");
            return;
        }

        // Clone the UXML asset into the scene's UI
        promptContainer = interactionPromptAsset.Instantiate();
        actionLabel = promptContainer.Q<Label>("ActionLabel");

        // Add the prompt to the root of the scene's UI but keep it hidden
        sceneUIDocument.rootVisualElement.Add(promptContainer);
        promptContainer.style.display = DisplayStyle.None;

        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // In LateUpdate, continuously update the prompt's screen position
        // to follow the target transform if it is active.
        if (promptContainer.style.display == DisplayStyle.Flex && currentTarget != null)
        {
            Vector2 screenPoint = RuntimePanelUtils.CameraTransformWorldToPanel(promptContainer.panel, currentTarget.position, mainCamera);
            promptContainer.transform.position = screenPoint;
        }
    }

    /// <summary>
    /// Displays the interaction prompt above a target object.
    /// </summary>
    /// <param name="targetTransform">The transform of the interactable object.</param>
    /// <param name="promptText">The dynamic text to display.</param>
    public void ShowPrompt(Transform targetTransform, string promptText)
    {
        this.currentTarget = targetTransform;
        actionLabel.text = promptText;
        promptContainer.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Hides the interaction prompt.
    /// </summary>
    public void HidePrompt()
    {
        this.currentTarget = null;
        promptContainer.style.display = DisplayStyle.None;
    }
}