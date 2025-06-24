// GameClient/Assets/Scripts/Enemy/AI/Strategies/Movement/OrbitPlayerStrategy.cs

using UnityEngine;

/// <summary>
/// A concrete implementation of MovementStrategy where the enemy attempts to
/// circle the player at a specified distance and speed.
/// </summary>
[CreateAssetMenu(fileName = "MS_OrbitPlayer_Standard", menuName = "Adaptive Survivors/AI Strategies/Movement/Orbit Player")]
public class OrbitPlayerStrategy : MovementStrategy
{
 
    [Header("Orbit Parameters")]
    [Tooltip("The ideal distance to maintain from the player.")]
    [SerializeField] private float orbitDistance = 8f;
    [Tooltip("How fast the enemy circles the player, as a multiplier of its base MoveSpeed.")]
    [SerializeField] private float orbitSpeedMultiplier = 1.0f;
    [Tooltip("The direction of orbit. 1 for clockwise, -1 for counter-clockwise.")]
    [SerializeField] private int orbitDirection = 1;

    /// <summary>
    /// Executes the movement logic to orbit the player.
    /// </summary>
    /// <param name="brain">The EnemyBrain of the enemy executing the strategy.</param>
    public override void Execute(EnemyBrain brain)
    {
        if (brain.TargetTransform == null) return;

        // Vector from enemy to player
        Vector2 toPlayer = brain.TargetTransform.position - brain.transform.position;

        // Get the perpendicular vector for tangential movement, scaled by the enemy's runtime speed
        float tangentialSpeed = brain.MoveSpeed * orbitSpeedMultiplier;
        Vector2 orbitVelocity = new Vector2(-toPlayer.y, toPlayer.x).normalized * tangentialSpeed * orbitDirection;

        // Add a correction force to maintain the orbit distance
        float distanceError = toPlayer.magnitude - orbitDistance;
        Vector2 correctionVelocity = toPlayer.normalized * distanceError * brain.MoveSpeed;

        // Combine velocities and apply to the Rigidbody using the correct property from EnemyBrain
        brain.Rigidbody.linearVelocity = orbitVelocity + correctionVelocity;
    }
}