// GameClient/Assets/Scripts/Enemy/AI/Strategies/Movement/DirectChaseStrategy.cs

using UnityEngine;

/// <summary>
/// A concrete MovementStrategy where the enemy moves directly towards its target.
/// The logic is adapted from the original EnemyMovement.cs.
/// </summary>
[CreateAssetMenu(fileName = "DirectChaseStrategy", menuName = "Adaptive Survivors/AI Strategies/Movement/Direct Chase")]
public class DirectChaseStrategy : MovementStrategy
{
    /// <summary>
    /// Executes the direct chase movement logic.
    /// It relies on the EnemyBrain to provide the necessary context (target, speed, Rigidbody).
    /// </summary>
    public override void Execute(EnemyBrain brain)
    {
        if (brain == null || brain.TargetTransform == null || brain.Rigidbody == null)
        {
            return;
        }

        // Calculate direction and apply velocity, as was done in EnemyMovement.FixedUpdate.
        Vector2 direction = (brain.TargetTransform.position - brain.transform.position).normalized;
        brain.Rigidbody.linearVelocity = direction * brain.MoveSpeed;
    }
}