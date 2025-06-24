// GameClient/Assets/Scripts/Enemy/AI/Strategies/Attack/ContactAttackStrategy.cs

using UnityEngine;

/// <summary>
/// A concrete AttackStrategy that deals damage on physical contact.
/// This strategy holds the damage value, but the trigger logic (OnCollisionEnter2D)
/// will reside in the EnemyBrain for a more robust implementation.
/// </summary>
[CreateAssetMenu(fileName = "ContactAttackStrategy", menuName = "Adaptive Survivors/AI Strategies/Attack/Contact Attack")]
public class ContactAttackStrategy : AttackStrategy
{
    /// <summary>
    /// As per the technical specification, the logic from EnemyAttack.cs (dealing damage on collision)
    /// is encapsulated by this strategy.
    /// However, since collision is an event (OnCollisionEnter2D) and not a continuous action,
    /// this Execute method is intentionally left empty.
    ///
    /// The EnemyBrain will be responsible for detecting the collision and then delegating
    /// the attack action to its current AttackStrategy. This is a more robust and flexible
    /// approach that still adheres to the Strategy Pattern's goal of separating the
    /// "what" (the attack logic/data) from the "who" (the enemy).
    /// </summary>
    public override void Execute(EnemyBrain brain)
    {
        // This strategy is reactive and is triggered by collision events handled in EnemyBrain.
        // Therefore, this per-frame Execute call is not needed for this specific strategy.
    }
}