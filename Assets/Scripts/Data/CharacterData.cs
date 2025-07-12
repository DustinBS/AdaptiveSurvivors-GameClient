// GameClient/Assets/Scripts/Data/CharacterData.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that defines the data for a character archetype.
/// This allows for easy creation and modification of character stats and details
/// as assets within the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Adaptive Survivors/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("The character's display name.")]
    public string characterName;

    [Tooltip("A short thematic description for the character selection screen.")]
    [TextArea(3, 10)]
    public string description;

    [Header("Visuals")]
    [Tooltip("The sprite used for the character's in-game appearance.")]
    public Sprite characterSprite;

    [Tooltip("The 'head in a jar' portrait for the character selection UI.")]
    public Sprite characterPortrait;

    [Tooltip("The weapon this character starts the run with.")]
    public WeaponData startingWeapon;

    [System.Serializable]
    public class BaseAttribute
    {
        public AttributeData attribute;
        public float value;
    }

    [Header("Base Stats")]
    public List<BaseAttribute> baseAttributes = new List<BaseAttribute>();
}
