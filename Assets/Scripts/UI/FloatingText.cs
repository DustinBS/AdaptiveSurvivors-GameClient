// GameClient/Assets/Scripts/UI/FloatingText.cs

using UnityEngine;
using TMPro; // Required for TextMeshPro

/// <summary>
/// Controls the behavior of a single floating combat text element.
/// It animates the text to move upwards and fade out over its lifetime.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class FloatingText : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How long the text will be visible before being destroyed.")]
    public float lifetime = 1f;
    [Tooltip("How fast the text moves upwards.")]
    public float moveSpeed = 1f;

    private TextMeshProUGUI textComponent;
    private float timeElapsed;
    private Color startColor;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        startColor = textComponent.color;
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;

        // Move the text upwards
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // Fade the text out over its lifetime
        float fadePercent = timeElapsed / lifetime;
        Color newColor = new Color(startColor.r, startColor.g, startColor.b, 1 - fadePercent);
        textComponent.color = newColor;

        // Deactivate the object when its lifetime is over
        if (timeElapsed >= lifetime)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Initializes the floating text with a damage value.
    /// </summary>
    /// <param name="damageAmount">The number to display.</param>
    public void SetText(float damageAmount)
    {
        textComponent.text = Mathf.CeilToInt(damageAmount).ToString();

        // Reset state for object pooling
        timeElapsed = 0;
        textComponent.color = startColor;
    }
}
