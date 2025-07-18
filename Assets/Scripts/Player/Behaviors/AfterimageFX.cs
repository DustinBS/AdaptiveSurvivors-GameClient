// GameClient/Assets/Scripts/Player/Behaviors/AfterimageFX.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the behavior of a single afterimage effect.
/// It fades out the sprite and then destroys the GameObject.
/// This component should be placed on the afterimage prefab.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AfterimageFX : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("How long it takes for the afterimage to fade completely.")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("The initial transparency of the sprite. 1 is fully opaque, 0 is fully transparent.")]
    [Range(0, 1)]
    [SerializeField] private float initialAlpha = 0.6f;

    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Initializes the afterimage with the player's current sprite and begins the fade-out process.
    /// </summary>
    /// <param name="sprite">The sprite to display for the afterimage.</param>
    public void Initialize(Sprite sprite)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("AfterimageFX requires a SpriteRenderer component.", this);
            Destroy(gameObject);
            return;
        }

        spriteRenderer.sprite = sprite;
        // Apply initial color and transparency
        Color tempColor = spriteRenderer.color;
        tempColor.a = initialAlpha;
        spriteRenderer.color = tempColor;

        StartCoroutine(FadeOutAndDestroy());
    }

    /// <summary>
    /// A coroutine that gradually fades the sprite's alpha to zero over the fadeDuration,
    /// then destroys the GameObject.
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // Linearly interpolate the color's alpha channel
            spriteRenderer.color = Color.Lerp(startColor, endColor, elapsedTime / fadeDuration);
            yield return null;
        }

        // Ensure it's fully destroyed after the loop
        Destroy(gameObject);
    }
}