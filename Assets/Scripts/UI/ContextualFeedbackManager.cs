// GameClient/Assets/Scripts/UI/ContextualFeedbackManager.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A singleton manager for displaying non-combat, contextual floating text.
/// Uses an object pool for efficiency.
/// </summary>
public class ContextualFeedbackManager : MonoBehaviour
{
    public static ContextualFeedbackManager Instance { get; private set; }

    [Header("Prefab & Pooling")]
    [Tooltip("The prefab for the feedback text object. Must have a FeedbackText component.")]
    [SerializeField] private GameObject feedbackTextPrefab;
    [Tooltip("The initial number of text objects to pool.")]
    [SerializeField] private int poolSize = 10;

    [Header("UI Parent")]
    [Tooltip("The parent transform for the text objects, typically the main UI Canvas.")]
    [SerializeField] private RectTransform canvasTransform;

    private List<FeedbackText> textPool = new List<FeedbackText>();
    private Camera mainCamera;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        mainCamera = Camera.main;

        if (canvasTransform == null)
        {
            Debug.LogError($"{nameof(ContextualFeedbackManager)}: Canvas Transform is not assigned.", this);
            enabled = false;
        }
    }

    void Start()
    {
        if (feedbackTextPrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                CreatePooledObject();
            }
        }
    }

    /// <summary>
    /// Public method to request a piece of feedback text at a world position.
    /// </summary>
    /// <param name="text">The string to display.</param>
    /// <param name="worldPosition">The world-space position to anchor the text to.</param>
    public void ShowFeedback(string text, Vector3 worldPosition)
    {
        FeedbackText textToShow = GetPooledObject();
        if (textToShow != null)
        {
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            textToShow.transform.position = screenPosition;
            textToShow.Show(text);
        }
    }

    private FeedbackText GetPooledObject()
    {
        foreach (FeedbackText text in textPool)
        {
            if (!text.gameObject.activeInHierarchy)
            {
                return text;
            }
        }
        return CreatePooledObject();
    }

    private FeedbackText CreatePooledObject()
    {
        if (feedbackTextPrefab == null) return null;
        GameObject newObj = Instantiate(feedbackTextPrefab, canvasTransform);
        FeedbackText feedbackText = newObj.GetComponent<FeedbackText>();
        newObj.SetActive(false);
        textPool.Add(feedbackText);
        return feedbackText;
    }
}