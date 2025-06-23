// GameClient/Assets/Scripts/Enemy/AI/Strategies/MovementStrategy.cs

using UnityEngine;

public abstract class MovementStrategy : AIStrategy
{
    [Header("Movement Settings")]
    [Tooltip("The base movement speed for this strategy.")]
    public float baseSpeed = 3f;
}