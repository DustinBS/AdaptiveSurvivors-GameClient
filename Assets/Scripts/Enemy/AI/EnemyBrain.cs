// GameClient/Assets/Scripts/Enemy/AI/EnemyBrain.cs

using UnityEngine;

/// <summary>
/// The central controller for an enemy's AI. This MonoBehaviour replaces individual
/// movement and attack scripts. It holds references to strategy ScriptableObjects
/// and delegates the execution of AI behaviors to them.
/// It also holds the enemy's runtime stats, which can be modified by other systems.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBrain : MonoBehaviour
{
    // --- Public Properties for Strategies ---
    // Strategies will access these properties to execute their logic.
    public Transform TargetTransform { get; private set; }
    public Rigidbody2D Rigidbody { get; private set; }
    public float MoveSpeed { get; set; } // Can be modified by adaptive systems
    public float Damage { get; set; }    // Can be modified by adaptive systems

    // --- Strategy References ---
    // These are assigned by the EnemySpawner from an EnemyData asset.
    private MovementStrategy movementStrategy;
    private AttackStrategy attackStrategy;

    private bool isInitialized = false;

    void Awake()
    {
        // Cache required components for performance.
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Initializes the EnemyBrain with its target, strategies, and initial stats.
    /// This single initialization point makes the EnemySpawner's job much cleaner.
    /// </summary>
    public void Initialize(Transform target, MovementStrategy moveStrat, AttackStrategy atkStrat, float initialMoveSpeed, float initialDamage)
    {
        this.TargetTransform = target;
        this.MoveSpeed = initialMoveSpeed;
        this.Damage = initialDamage;

        this.movementStrategy = moveStrat;
        this.attackStrategy = atkStrat;

        isInitialized = true;
    }

    /// <summary>
    /// We use FixedUpdate for movement to ensure smooth, physics-based motion.
    /// </summary>
    void FixedUpdate()
    {
        if (!isInitialized || movementStrategy == null) return;

        // Delegate movement behavior to the assigned movement strategy.
        movementStrategy.Execute(this);
    }

    /// <summary>
    /// Update can be used for non-physics based logic.
    /// </summary>
    void Update()
    {
        if (!isInitialized || attackStrategy == null) return;

        // Delegate attack behavior to the assigned attack strategy.
        // For our reactive ContactAttackStrategy, this does nothing, but for a
        // ranged enemy, this is where it would decide when to fire.
        attackStrategy.Execute(this);
    }

    /// <summary>
    /// Handles collision-based attacks, centralizing the logic from the old EnemyAttack.cs.
    /// This is the "reactive" part of our ContactAttackStrategy.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized) return;

        // This check is the key to our flexible system. We only attempt a contact attack
        // if the enemy's assigned strategy is actually a ContactAttackStrategy.
        // A ranged enemy with a different attack strategy wouldn't deal damage on collision.
        if (attackStrategy is ContactAttackStrategy)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                // Using TryGetComponent is slightly more performant than GetComponent.
                if (collision.gameObject.TryGetComponent<PlayerStatus>(out var playerStatus))
                {
                    // Use the Damage property of this brain, which was initialized by the spawner.
                    playerStatus.TakeDamage(this.Damage, gameObject.name);
                }
            }
        }
    }
}