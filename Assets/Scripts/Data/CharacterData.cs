// GameClient/Assets/Scripts/Data/CharacterData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New CharacterData", menuName = "Adaptive Survivors/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character Info")]
    public string characterName;
    [TextArea(3, 5)]
    public string description;
    public Sprite characterSprite; // For the character select screen
    public Sprite characterPortrait; // For the dialogue UI

    [Header("Base Stats")]
    public float baseHealth = 100f;
    public float baseDamage = 10f;

    [Header("Stat Multipliers")]
    public float speedMultiplier = 1f;

    [Header("Unique Abilities")]
    public bool hasHealthRegen = false;
    [Tooltip("If hasHealthRegen is true, this is the percentage of max health to regen per second.")]
    public float healthRegenPercent = 0.01f;

    public bool hasExtraUpgradeChoice = false;
    [Tooltip("If hasExtraUpgradeChoice is true, this is the number of extra choices.")]
    public int extraUpgradeChoices = 1;

    [Header("Default Settings")]
    [Tooltip("The default number of upgrade choices offered to this character.")]
    public int defaultUpgradeChoices = 2;
}
