// GameClient/Assets/Scripts/Enemy/AI/Strategies/Attack/NoAttackStrategy.cs

using UnityEngine;

/// <summary>
/// A concrete implementation of AttackStrategy that performs no action.
/// Useful for passive enemies like the Vector Vexer that do not have a direct attack.
/// </summary>
[CreateAssetMenu(fileName = "AS_NoAttack_Standard", menuName = "Adaptive Survivors/AI Strategies/Attack/No Attack")]
public class NoAttackStrategy : AttackStrategy
{
    /// <summary>
    /// Executes the attack logic. For this strategy, it does nothing.
    /// </summary>
    /// <param name="brain">The EnemyBrain of the enemy executing the strategy.</param>
    public override void Execute(EnemyBrain brain)
    {
        // This strategy intentionally does nothing.
        return;
    }
}