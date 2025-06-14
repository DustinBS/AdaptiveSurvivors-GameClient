// GameClient/Assets/Scripts/Data/EnemyData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Adaptive Survivors/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string enemyID;
    public string enemyName;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
    public float damage = 10f; // [FIX] Added damage field
    public float attackSpeed = 1f; // [FIX] Added attackSpeed field (attacks per second)
    public float xpValue = 10f;

    [Header("Visuals")]
    public GameObject enemyPrefab;
}
