// GameClient/Assets/Scripts/UI/CombatTextManager.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A singleton manager responsible for creating and displaying floating combat text.
/// It listens for damage events and uses an object pool for efficient text spawning.
/// </summary>
public class CombatTextManager : MonoBehaviour
{
    public static CombatTextManager Instance { get; private set; }

    [Header("Prefab & Pooling")]
    [Tooltip("The prefab for the floating text object. Must have a FloatingText component.")]
    [SerializeField] private GameObject floatingTextPrefab;
    [Tooltip("The initial number of text objects to pool.")]
    [SerializeField] private int poolSize = 20;

    // --- NEW: Reference to the UI Canvas ---
    [Header("UI Parent")]
    [Tooltip("The parent transform for the text objects, typically the main UI Canvas.")]
    [SerializeField] private RectTransform canvasTransform;

    private List<FloatingText> textPool = new List<FloatingText>();
    private Camera mainCamera; // Cache the camera for performance

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        mainCamera = Camera.main;

        // --- NEW: Error check for the canvas reference ---
        if (canvasTransform == null)
        {
            Debug.LogError("CombatTextManager: Canvas Transform is not assigned. Please assign the main UI Canvas in the Inspector.", this);
            enabled = false;
        }
    }

    void Start()
    {
        // Create the object pool
        if (floatingTextPrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                CreatePooledObject();
            }
        }
    }

    void OnEnable()
    {
        // Subscribe to the damage event
        EnemyHealth.OnDamaged += ShowFloatingText;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EnemyHealth.OnDamaged -= ShowFloatingText;
    }

    /// <summary>
    /// The callback function that handles the OnDamaged event.
    /// </summary>
    private void ShowFloatingText(float damageAmount, Vector3 worldPosition)
    {
        FloatingText textToShow = GetPooledObject();
        if (textToShow != null)
        {
            // Position the UI element on the screen based on the enemy's world position
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            textToShow.transform.position = screenPosition;

            textToShow.gameObject.SetActive(true);
            textToShow.SetText(damageAmount);
        }
    }

    /// <summary>
    /// Retrieves an inactive FloatingText object from the pool.
    /// </summary>
    private FloatingText GetPooledObject()
    {
        foreach (FloatingText text in textPool)
        {
            if (!text.gameObject.activeInHierarchy)
            {
                return text;
            }
        }
        // If the pool is exhausted, create a new object and add it to the pool
        return CreatePooledObject();
    }

    /// <summary>
    /// Creates a new FloatingText instance and adds it to the pool.
    /// </summary>
    private FloatingText CreatePooledObject()
    {
        GameObject newObj = Instantiate(floatingTextPrefab, canvasTransform);
        FloatingText floatingText = newObj.GetComponent<FloatingText>();
        newObj.SetActive(false);
        textPool.Add(floatingText);
        return floatingText;
    }
}
