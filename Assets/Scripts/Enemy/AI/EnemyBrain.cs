// GameClient/Assets/Scripts/Enemy/AI/EnemyBrain.cs

using UnityEngine;

/// <summary>
/// The central controller for an enemy's AI. It now holds base stats and can have
/// multipliers applied to them by the adaptive system.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBrain : MonoBehaviour
{
    // --- Public Properties ---
    public Transform TargetTransform { get; private set; }
    public Rigidbody2D Rigidbody { get; private set; }

    // --- Runtime Stats (can be modified) ---
    public float MoveSpeed { get; private set; }
    public float Damage { get; private set; }

    // --- Private Base Stats ---
    private float baseMoveSpeed;
    private float baseDamage;

    // --- Strategy References ---
    private MovementStrategy movementStrategy;
    private AttackStrategy attackStrategy;

    private bool isInitialized = false;

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Transform target, MovementStrategy moveStrat, AttackStrategy atkStrat, float initialMoveSpeed, float initialDamage)
    {
        this.TargetTransform = target;

        // Store the initial stats from the strategy assets as our base stats
        this.baseMoveSpeed = initialMoveSpeed;
        this.baseDamage = initialDamage;

        // Set the current runtime stats to the base values initially
        this.MoveSpeed = baseMoveSpeed;
        this.Damage = baseDamage;

        this.movementStrategy = moveStrat;
        this.attackStrategy = atkStrat;

        isInitialized = true;
    }

    // --- Public Methods for Adaptive System ---

    /// <summary>
    /// Applies a multiplier to the base movement speed.
    /// </summary>
    /// <param name="multiplier">The factor to multiply speed by (e.g., 1.5 for a 50% increase).</param>
    public void ApplySpeedMultiplier(float multiplier)
    {
        MoveSpeed = baseMoveSpeed * multiplier;
    }

    /// <summary>
    /// Applies a multiplier to the base damage.
    /// </summary>
    /// <param name="multiplier">The factor to multiply damage by (e.g., 1.2 for a 20% increase).</param>
    public void ApplyDamageMultiplier(float multiplier)
    {
        Damage = baseDamage * multiplier;
    }

    /// <summary>
    /// Resets speed and damage back to their original base values.
    /// </summary>
    public void ResetStatMultipliers()
    {
        MoveSpeed = baseMoveSpeed;
        Damage = baseDamage;
    }


    void FixedUpdate()
    {
        if (!isInitialized || movementStrategy == null) return;
        movementStrategy.Execute(this);
    }
    
    void Update()
    {
        if (!isInitialized || attackStrategy == null) return;
        attackStrategy.Execute(this);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized) return;

        if (attackStrategy is ContactAttackStrategy)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                if (collision.gameObject.TryGetComponent<PlayerStatus>(out var playerStatus))
                {
                    playerStatus.TakeDamage(this.Damage, gameObject.name);
                }
            }
        }
    }
}