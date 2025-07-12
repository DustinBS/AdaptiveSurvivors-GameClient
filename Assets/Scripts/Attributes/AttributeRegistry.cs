// GameClient/Assets/Scripts/Attributes/AttributeRegistry.cs
using UnityEngine;

/// <summary>
/// A static registry ScriptableObject to hold easy-to-access references
/// to all core AttributeData assets. This avoids manual linking in multiple
/// prefabs and scripts. Populate this single asset in the editor.
/// </summary>
[CreateAssetMenu(fileName = "AttributeRegistry", menuName = "Adaptive Survivors/Attributes/Attribute Registry")]
public class AttributeRegistry : ScriptableObject
{
    // --- Player Stats ---
    public AttributeData MaxHealth;
    public AttributeData HealthRegen;
    public AttributeData MoveSpeed;
    public AttributeData Armor;
    public AttributeData MagnetRange;
    public AttributeData MaxMana;
    public AttributeData ManaRegen;


    // --- Weapon Stats ---
    public AttributeData CharacterDamageMultiplier;
    public AttributeData GlobalDamageMultiplier;
    public AttributeData BaseDamage;
    public AttributeData AttackSpeed;
    public AttributeData AttackRange;
    public AttributeData ProjectileCount;

    // --- Misc Stats ---
    public AttributeData CooldownReduction;
    public AttributeData DefaultUpgradeChoices;
    public AttributeData ExtraUpgradeChoices;
}