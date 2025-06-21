// GameClient/Assets/Scripts/Data/CharacterData.cs
using UnityEngine;

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

    [Header("Core Stats")]
    [Tooltip("The character's starting health.")]
    public float baseHealth = 100f;

    [Tooltip("The character's base damage output.")]
    public float baseDamage = 10f;

    [Tooltip("A multiplier for the character's movement speed.")]
    [Range(0.5f, 2.0f)]
    public float speedMultiplier = 1.0f;

    [Header("Unique Mechanics")]
    [Tooltip("Does this character have innate health regeneration?")]
    public bool hasHealthRegen = false;

    [Tooltip("Percentage of max health regenerated per second. Only active if hasHealthRegen is true.")]
    [Range(0f, 10f)]
    public float healthRegenPercent = 1.0f;

    [Tooltip("The standard number of upgrade choices offered.")]
    public int defaultUpgradeChoices = 2;

    [Tooltip("How many BONUS upgrade choices does this character get?")]
    public int extraUpgradeChoices = 0;
}
