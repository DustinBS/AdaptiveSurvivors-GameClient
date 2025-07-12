// GameClient/Assets/Scripts/Attributes/AttributeData.cs
using UnityEngine;

/// <summary>
/// A ScriptableObject that defines a single, unique gameplay attribute (e.g., "Max Health", "Move Speed").
/// This asset acts as both an identifier and a container for the attribute's display name.
/// </summary>
[CreateAssetMenu(fileName = "Attribute_", menuName = "Adaptive Survivors/Attributes/Attribute Data")]
public class AttributeData : ScriptableObject
{
    [Tooltip("The unique ID for this attribute. Used for saving, loading, and backend events.")]
    public string id;

    [Tooltip("The name displayed to the player in UI elements.")]
    public string displayName;
}