// GameClient/Assets/Scripts/Data/EliteData.cs

using UnityEngine;

/// <summary>
/// A specialized ScriptableObject for Elite enemies. It inherits from EnemyData
/// and allows for elite-specific properties, such as unique abilities, resistances,
/// or modified visual effects.
/// </summary>
[CreateAssetMenu(fileName = "NewEliteData", menuName = "Adaptive Survivors/Elite Data")]
public class EliteData : EnemyData
{
    // As with BossData, this class is primarily for type identification for now.
    // We can add elite-specific fields here later based on the GDD, such as:
    //
    // [Header("Elite Mechanics")]
    // [Range(0f, 1f)]
    // public float baseWeaponResistance = 0.5f;
    // public SpecialEffect aoeOnDeathEffect;
}