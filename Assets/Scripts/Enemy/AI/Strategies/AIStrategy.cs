// GameClient/Assets/Scripts/Enemy/AI/Strategies/AIStrategy.cs

using UnityEngine;

/// <summary>
/// Abstract base class for all enemy AI behaviors, designed as a ScriptableObject.
/// This allows AI behaviors to be created and configured as assets.
/// </summary>
public abstract class AIStrategy : ScriptableObject
{
    /// <summary>
    /// Executes the AI behavior logic. This method is called by the EnemyBrain.
    /// </summary>
    /// <param name="brain">The EnemyBrain controller that is executing this strategy.</param>
    public abstract void Execute(EnemyBrain brain);
}