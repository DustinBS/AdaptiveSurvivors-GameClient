// GameClient/Assets/Scripts/Data/WeaponData.cs

using UnityEngine;

/// <summary>
/// Defines the static properties of a weapon using a ScriptableObject.
/// This allows for creating different weapons like swords, wands, etc., as assets in the project.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Adaptive Survivors/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Core Identification")]
    [Tooltip("A unique identifier for this weapon (e.g., 'starter_sword', 'fire_wand'). Used for backend event tracking.")]
    public string weaponID;

    [Tooltip("Display name for the weapon.")]
    public string weaponName;

    [Header("Attack Properties")]
    [Tooltip("The base damage dealt by a single hit from this weapon.")]
    public float baseDamage = 10f;

    [Tooltip("The time in seconds between each attack.")]
    public float attackInterval = 1.0f;

    [Tooltip("The radius within which this weapon can detect and attack enemies.")]
    public float attackRange = 5.0f;

    [Header("Projectile Settings")]
    [Tooltip("Is this a projectile-based weapon? If false, it's treated as a melee attack.")]
    public bool isProjectile;

    [Tooltip("The prefab to instantiate for the projectile. Only used if isProjectile is true.")]
    public GameObject projectilePrefab;
}
