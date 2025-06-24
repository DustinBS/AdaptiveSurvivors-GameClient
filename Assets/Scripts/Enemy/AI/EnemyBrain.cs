// GameClient/Assets/Scripts/Enemy/AI/EnemyBrain.cs

using UnityEngine;
using System;

/// <summary>
/// The central controller for an enemy's AI. Manages the initialization sequence
/// and broadcasts an event when the enemy and its core components are fully ready.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(EnemyHealth))]
public class EnemyBrain : MonoBehaviour
{
    // --- Events ---
    /// <summary>
    /// Fired after the brain and all dependent components have been initialized.
    /// Other components should subscribe to this to safely begin their logic.
    /// </summary>
    public event Action OnInitialized;

    // --- Public Properties ---
    public Transform TargetTransform { get; private set; }
    public Rigidbody2D Rigidbody { get; private set; }
    public EnemyHealth Health { get; private set; }
    public EnemyData Data { get; private set; }

    // --- Runtime Stats ---
    public float MoveSpeed { get; private set; }
    public float Damage { get; private set; }

    // --- Base Stats ---
    private float baseMoveSpeed;
    private float baseDamage;

    // --- Strategy References ---
    private MovementStrategy movementStrategy;
    private AttackStrategy attackStrategy;

    private bool isInitialized = false;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
        Health = GetComponent<EnemyHealth>(); // Cache reference to required Health component.
    }

    /// <summary>
    /// Primary, centralized initialization method for the enemy entity.
    /// Sets up all core components and invokes the OnInitialized event when complete.
    /// </summary>
    /// <param name="target">The player or target the enemy should pursue.</param>
    /// <param name="enemyData">The ScriptableObject defining this enemy's properties.</param>
    public void Initialize(Transform target, EnemyData enemyData)
    {
        if (isInitialized) return;

        // --- Pre-condition Checks ---
        if (enemyData.movementStrategy == null || enemyData.attackStrategy == null)
        {
            Debug.LogError($"EnemyData '{enemyData.name}' is missing a required AI Strategy. Initialization failed.", this);
            gameObject.SetActive(false); // Defensively disable to prevent runtime errors.
            return;
        }

        // --- State Initialization ---
        this.TargetTransform = target;
        this.movementStrategy = enemyData.movementStrategy;
        this.attackStrategy = enemyData.attackStrategy;
        this.Data = enemyData;

        // --- Stat Initialization ---
        this.baseMoveSpeed = movementStrategy.baseSpeed;
        this.baseDamage = attackStrategy.baseDamage;
        this.MoveSpeed = baseMoveSpeed;
        this.Damage = baseDamage;

        // --- Controlled Component Initialization ---
        // Initialize other critical components in a deterministic order.
        Health.Initialize(enemyData);

        isInitialized = true;

        // --- Broadcast Readiness ---
        // Fire the event. If no components are subscribed, it will do nothing.
        // This makes the system robust for both adaptive and non-adaptive enemies.
        OnInitialized?.Invoke();
    }

    /// <summary>
    /// Applies a multiplier to the base movement speed.
    /// </summary>
    /// <param name="multiplier">The factor to multiply speed by (e.g., 1.5 for +50%).</param>
    public void ApplySpeedMultiplier(float multiplier)
    {
        MoveSpeed = baseMoveSpeed * multiplier;
    }

    /// <summary>
    /// Applies a multiplier to the base damage.
    /// </summary>
    /// <param name="multiplier">The factor to multiply damage by (e.g., 1.2 for +20%).</param>
    public void ApplyDamageMultiplier(float multiplier)
    {
        Damage = baseDamage * multiplier;
    }

    /// <summary>
    /// Resets runtime stats back to their original base values.
    /// </summary>
    public void ResetStatMultipliers()
    {
        MoveSpeed = baseMoveSpeed;
        Damage = baseDamage;
    }

    private void FixedUpdate()
    {
        if (!isInitialized || movementStrategy == null) return;
        movementStrategy.Execute(this);
    }

    private void Update()
    {
        if (!isInitialized || attackStrategy == null) return;
        attackStrategy.Execute(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized || !(attackStrategy is ContactAttackStrategy)) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<PlayerStatus>(out var playerStatus))
            {
                playerStatus.TakeDamage(this.Damage, gameObject.name);
            }
        }
    }
}