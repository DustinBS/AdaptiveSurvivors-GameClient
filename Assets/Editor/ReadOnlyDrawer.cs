// GameClient/Assets/Editor/ReadOnlyDrawer.cs

using UnityEngine;
using UnityEditor;

/// <summary>
/// This class is a custom Property Drawer for the [ReadOnly] attribute.
/// It finds any field with the [ReadOnly] attribute and disables the GUI for it,
/// making it visible but not editable in the Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // First, disable the GUI. This makes all subsequent controls non-interactive.
        GUI.enabled = false;

        // Then, draw the property field as it would normally be drawn.
        // Because the GUI is disabled, it will appear "grayed out" and be non-editable.
        EditorGUI.PropertyField(position, property, label, true);

        // Finally, re-enable the GUI for any subsequent fields that need to be drawn.
        GUI.enabled = true;
    }
}