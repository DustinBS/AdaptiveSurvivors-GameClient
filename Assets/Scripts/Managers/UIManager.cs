// GameClient/Assets/Scripts/Managers/UIManager.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// A scene-specific singleton that manages and controls all major UI panels.
/// It is responsible for showing, hiding, and animating UI elements like the death menu,
/// pause menu, dialogue windows, etc.
/// </summary>
public class UIManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static UIManager Instance { get; private set; }

    [Header("UI Panel References")]
    [Tooltip("The parent GameObject for the Death Menu UI panel.")]
    [SerializeField] private GameObject deathMenuPanel;

    // We can add references to other panels here later (e.g., PauseMenu, DialoguePanel)

    void Awake()
    {
        // Scene-specific singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Ensure all panels are hidden by default on start
        if (deathMenuPanel != null)
        {
            deathMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the Death Menu with a fade-in animation.
    /// </summary>
    public void ShowDeathMenu()
    {
        if (deathMenuPanel != null)
        {
            StartCoroutine(FadeInPanel(deathMenuPanel, 0.5f));
        }
    }

    /// <summary>
    /// A reusable coroutine to fade a UI panel in.
    /// It requires the panel's root GameObject to have a CanvasGroup component.
    /// </summary>
    /// <param name="panel">The GameObject of the panel to fade in.</param>
    /// <param name="duration">How long the fade animation should take.</param>
    private IEnumerator FadeInPanel(GameObject panel, float duration)
    {
        // Ensure the panel has a CanvasGroup component
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        // Set initial state
        panel.SetActive(true);
        canvasGroup.alpha = 0f;

        // Animate the alpha value over time
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            // Use unscaledDeltaTime to ensure the animation runs even when the game is "paused"
            timeElapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timeElapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f; // Ensure it's fully visible at the end
    }

    // We can add other methods here later, like HideDeathMenu(), ShowPauseMenu(), etc.
}
