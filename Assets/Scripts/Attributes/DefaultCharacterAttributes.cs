// GameClient/Assets/Scripts/Attributes/DefaultCharacterAttributes.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that holds the default starting attributes for any character.
/// This reduces boilerplate by allowing CharacterData assets to only specify values
/// that override these defaults.
/// </summary>
[CreateAssetMenu(fileName = "DefaultCharacterAttributes", menuName = "Adaptive Survivors/Attributes/Default Character Attributes")]
public class DefaultCharacterAttributes : ScriptableObject
{
    public List<CharacterData.BaseAttribute> defaultAttributes;
}