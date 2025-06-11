// GameClient/Assets/Scripts/Data/UpgradeData.cs

using UnityEngine;

/// <summary>
/// Enum to define the types of stats or properties an upgrade can affect.
/// This provides a clear, dropdown-selectable list in the Inspector and avoids "magic strings".
/// </summary>
public enum UpgradeType
{
    // Player Stats
    MaxHealth,
    HealthRegen,
    MoveSpeed,
    Armor,
    MagnetRange,

    // Weapon Stats
    WeaponDamage,
    AttackSpeed, // This would translate to modifying attackInterval
    AttackRange,
    ProjectileCount,

    // Misc
    CooldownReduction
}


/// <summary>
/// Defines a single player upgrade using a ScriptableObject.
/// This allows for creating a pool of different upgrades as assets in the project.
/// </summary>
[CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Adaptive Survivors/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Core Identification")]
    [Tooltip("A unique identifier for this upgrade (e.g., 'player_speed_1', 'weapon_dmg_1'). Used for backend event tracking.")]
    public string upgradeID;

    [Header("UI Display")]
    [Tooltip("The title of the upgrade shown to the player (e.g., 'Swift Boots').")]
    public string title;

    [Tooltip("A brief description of what the upgrade does.")]
    [TextArea(3, 5)]
    public string description;

    [Tooltip("The icon to display in the upgrade selection UI.")]
    public Sprite icon;

    [Header("Gameplay Effect")]
    [Tooltip("The type of stat or property this upgrade affects.")]
    public UpgradeType upgradeType;

    [Tooltip("The value to modify the stat by. Can be a flat value or a percentage.")]
    public float value;

    [Tooltip("Is the 'value' a percentage modifier? If true, 0.1 = +10%. If false, it's a flat addition.")]
    public bool isPercentage;
}
