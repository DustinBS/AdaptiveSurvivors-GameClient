// GameClient/Assets/Scripts/Utility/ReadOnlyAttribute.cs

using UnityEngine;

/// <summary>
/// A property attribute that allows a field to be displayed as read-only in the Unity Inspector.
/// The field is still serialized and visible, but it cannot be edited via the Inspector.
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute
{
}