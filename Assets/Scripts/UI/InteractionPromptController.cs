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
    private Transform _currentTarget; // The world object to follow.
    [SerializeField] private Vector3 _worldOffset = new Vector3(0, -1.0f, 0); // Default offset below the target.

    private void Awake()
    {
        _mainCamera = Camera.main;

        var uiDocument = GetComponent<UIDocument>();

        if (uiDocument.rootVisualElement == null)
        {
            Debug.LogError("InteractionPromptController: No Root Visual Element found on UIDocument.", this);
            enabled = false;
            return;
        }

        _promptContainer = uiDocument.rootVisualElement.Q<VisualElement>("InteractionPrompt");
        _iconElement = _promptContainer?.Q<VisualElement>("Icon");
        _actionTextLabel = _promptContainer?.Q<Label>("ActionText");

        if (_promptContainer == null || _iconElement == null || _actionTextLabel == null)
        {
            Debug.LogError("InteractionPromptController: Could not find all required UI elements in UXML.", this);
            enabled = false;
            return;
        }

        _promptContainer.style.opacity = 0;
        _promptContainer.style.visibility = Visibility.Hidden;
    }

    private void LateUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                return;
            }
        }

        if (_currentTarget != null && _promptContainer.style.opacity == 1)
        {
            Vector2 screenPoint = _mainCamera.WorldToScreenPoint(_currentTarget.position + _worldOffset);
            screenPoint.y = Screen.height - screenPoint.y;

            _promptContainer.style.left = screenPoint.x;
            _promptContainer.style.top = screenPoint.y;
        }
    }

    public void ShowPrompt(string promptText, Transform target)
    {
        if (target == null)
        {
             HidePrompt();
             return;
        }

        _currentTarget = target;

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
        _currentTarget = null;

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
