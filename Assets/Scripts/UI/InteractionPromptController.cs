// GameClient/Assets/Scripts/UI/InteractionPromptController.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Manages a world-space interaction prompt. It positions itself based on a target
/// Transform and displays a dynamic message. Styling is handled via USS.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class InteractionPromptController : MonoBehaviour
{
    // --- UI Elements ---
    private VisualElement _promptContainer;
    private VisualElement _iconElement;
    private Label _actionTextLabel;

    // --- Addressables ---
    private AsyncOperationHandle<Sprite> _spriteLoadHandle;
    private const string DefaultIconAddress = "icon_E";

    // --- Positioning ---
    private Camera _mainCamera;
    [SerializeField] private Vector3 _worldOffset = new Vector3(0, -1.0f, 0);

    private void Awake()
    {
        _mainCamera = Camera.main; 
        
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument.rootVisualElement == null) {
            Debug.LogError("InteractionPromptController: No Root Visual Element found on UIDocument.", this);
            enabled = false;
            return;
        }

        _promptContainer = uiDocument.rootVisualElement.Q<VisualElement>("InteractionPrompt");
        _iconElement = _promptContainer?.Q<VisualElement>("Icon");
        _actionTextLabel = _promptContainer?.Q<Label>("ActionText");

        if (_promptContainer == null || _iconElement == null || _actionTextLabel == null) {
            Debug.LogError("InteractionPromptController: Could not find all required UI elements in UXML.", this);
            enabled = false;
            return;
        }
        
        _promptContainer.style.opacity = 0;
        _promptContainer.style.visibility = Visibility.Hidden;
    }

    /// <summary>
    /// Updates the screen position of the prompt to follow a world-space transform.
    /// This should be called from LateUpdate to prevent jitter.
    /// </summary>
    /// <param name="target">The world-space transform to follow.</param>
    public void UpdatePosition(Transform target)
    {
        if (target == null || _promptContainer.style.opacity == 0) return;
        
        if (_mainCamera == null) {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return; 
        }
        
        Vector2 screenPoint = _mainCamera.WorldToScreenPoint(target.position + _worldOffset);
        // UI Toolkit positions from the top-left, so we must flip the y-coordinate.
        screenPoint.y = Screen.height - screenPoint.y;

        // Use translate to position the element, which is generally better for performance.
        _promptContainer.transform.position = new Vector3(screenPoint.x, screenPoint.y, 0);
    }
    
    /// <summary>
    /// Shows the prompt with the specified text. It no longer needs the transform.
    /// </summary>
    public void ShowPrompt(string promptText)
    {
        if (_promptContainer.style.opacity == 1 && _actionTextLabel.text == promptText)
        {
            return;
        }

        _actionTextLabel.text = promptText;
        _promptContainer.BringToFront();

        if (_spriteLoadHandle.IsValid())
        {
            Addressables.Release(_spriteLoadHandle);
        }
        
        _spriteLoadHandle = Addressables.LoadAssetAsync<Sprite>(DefaultIconAddress);
        _spriteLoadHandle.Completed += OnSpriteLoaded;
    }

    public void HidePrompt()
    {
        if (_promptContainer.style.opacity == 0) return;

        _promptContainer.style.opacity = 0;
        _promptContainer.style.visibility = Visibility.Hidden;

        if (_spriteLoadHandle.IsValid())
        {
            Addressables.Release(_spriteLoadHandle);
        }
    }

    private void OnSpriteLoaded(AsyncOperationHandle<Sprite> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _iconElement.style.backgroundImage = new StyleBackground(handle.Result);
            _promptContainer.style.visibility = Visibility.Visible;
            _promptContainer.style.opacity = 1;
        }
        else
        {
            Debug.LogError($"[Addressables] Failed to load sprite at address '{DefaultIconAddress}'. " +
                           $"Error: {handle.OperationException}. Please ensure you have run a new Addressables build via " +
                           $"'Window > Asset Management > Addressables > Build'. Also check that the asset's Texture Type is 'Sprite (2D and UI)'.", this);
        }
    }

    private void OnDestroy()
    {
        if (_spriteLoadHandle.IsValid())
        {
            Addressables.Release(_spriteLoadHandle);
        }
    }
}
