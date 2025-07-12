// GameClient/Assets/Scripts/Data/UpgradeData.cs

using UnityEngine;

// The UpgradeType enum is no longer needed and has been removed.

/// <summary>
/// Defines a single player upgrade using a ScriptableObject.
/// This allows for creating a pool of different upgrades as assets in the project.
/// This version is now fully data-driven, linking to AttributeData assets.
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

    [Header("Stat Effect")]
    [Tooltip("The attribute this upgrade modifies. Leave null if this is a behavior-only upgrade.")]
    public AttributeData attributeToModify;

    [Tooltip("The value to modify the stat by. Can be a flat value or a percentage.")]
    public float value;

    [Tooltip("Is the 'value' a percentage modifier? If true, 0.1 = +10%. If false, it's a flat addition.")]
    public bool isPercentage;

    [Header("Behavioral Effect")]
    [Tooltip("A prefab containing a component that grants a new behavior. Instantiated and added to the player when chosen.")]
    public GameObject behaviorComponentPrefab;

    [Header("Behavior")]
    [Tooltip("Can this upgrade be offered multiple times after it has been chosen once? (e.g., for generic stat boosts like '+10 Health').")]
    public bool isRepeatable = true;
}