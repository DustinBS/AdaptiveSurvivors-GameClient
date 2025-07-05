// GameClient/Assets/Scripts/Data/BossData.cs

using UnityEngine;

/// <summary>
/// A specialized ScriptableObject for bosses. It inherits from EnemyData
/// and can be extended with boss-specific properties in the future,
/// such as unique reward pools or multi-stage fight parameters.
/// </summary>
[CreateAssetMenu(fileName = "NewBossData", menuName = "Adaptive Survivors/Boss Data")]
public class BossData : EnemyData
{
    // For now, this class is primarily used for type identification.
    // We can add boss-specific fields here later, such as:
    //
    // [Header("Boss Mechanics")]
    // public GameObject[] bossFightArenas;
    // public float phaseTwoHealthThreshold;
    // public EnemyData[] reinforcementMinions;
}