// GameClient/Assets/Scripts/Enemy/AI/Strategies/AttackStrategy.cs

using UnityEngine;

public abstract class AttackStrategy : AIStrategy
{
    [Header("Attack Settings")]
    [Tooltip("The base damage dealt by this attack strategy.")]
    public float baseDamage = 10f;
}