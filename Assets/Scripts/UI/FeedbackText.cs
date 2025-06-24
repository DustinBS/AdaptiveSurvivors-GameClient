// GameClient/Assets/Scripts/UI/FeedbackText.cs

using UnityEngine;
using TMPro; // Use TextMeshPro for better text rendering
using System.Collections;

/// <summary>
/// Controls the behavior of a single piece of floating contextual feedback.
/// Manages its own fade-in/fade-out animation and deactivates itself for pooling.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI), typeof(CanvasGroup))]
public class FeedbackText : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private Vector2 moveDistance = new Vector2(0, 50f);

    private TextMeshProUGUI textComponent;
    private CanvasGroup canvasGroup;
    private Vector2 initialPosition;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Shows the feedback text and starts its animation lifecycle.
    /// </summary>
    /// <param name="text">The text to display.</param>
    public void Show(string text)
    {
        textComponent.text = text;
        gameObject.SetActive(true);
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        // --- Initialization and Fade In ---
        canvasGroup.alpha = 0f;
        initialPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float progress = elapsedTime / fadeDuration;
            canvasGroup.alpha = progress;
            transform.position = initialPosition + (moveDistance * progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // --- Hold ---
        yield return new WaitForSeconds(lifetime - (2 * fadeDuration));

        // --- Fade Out ---
        elapsedTime = 0f;
        Vector2 startFadeOutPosition = transform.position;

        while (elapsedTime < fadeDuration)
        {
            float progress = elapsedTime / fadeDuration;
            canvasGroup.alpha = 1f - progress;
            transform.position = startFadeOutPosition + (moveDistance * progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- Deactivate for Pooling ---
        gameObject.SetActive(false);
    }
}