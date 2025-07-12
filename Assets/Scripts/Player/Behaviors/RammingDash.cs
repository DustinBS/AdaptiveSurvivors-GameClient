// GameClient/Assets/Scripts/Player/Behaviors/RammingDash.cs
using UnityEngine;

/// <summary>
/// A self-contained behavior that damages enemies when the player dashes.
/// This component is added to the player when they select the corresponding upgrade.
/// </summary>
public class RammingDash : MonoBehaviour
{
    [Header("Ramming Settings")]
    [Tooltip("Base damage dealt by the ramming attack.")]
    [SerializeField] private float ramDamage = 25f;

    [Tooltip("The radius around the player where enemies will be damaged.")]
    [SerializeField] private float ramRadius = 1.5f;

    [Tooltip("The layer(s) that contain enemy characters.")]
    [SerializeField] private LayerMask enemyLayer;

    private PlayerStats playerStats;
    private AttributeRegistry attributeRegistry; // To get stat definitions

    void Awake()
    {
        // Get references needed for the behavior
        playerStats = GetComponent<PlayerStats>();

        // It's better to load a ScriptableObject from Resources or have it injected
        // than to use Find... for it, but for simplicity, we'll find it.
        attributeRegistry = Resources.FindObjectsOfTypeAll<AttributeRegistry>()[0];
    }

    void OnEnable()
    {
        // Subscribe to the event from PlayerMovement
        PlayerMovement.OnPlayerDashed += ExecuteRamAttack;
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent errors
        PlayerMovement.OnPlayerDashed -= ExecuteRamAttack;
    }

    private void ExecuteRamAttack(Vector2 dashDirection)
    {
        // --- Damage Calculation ---
        // 1. Get the base ramDamage.
        float baseDamage = ramDamage;

        // 2. Get the multipliers from our attribute system.
        float charMultiplier = playerStats.GetAttributeValue(attributeRegistry.CharacterDamageMultiplier);
        float globalMultiplier = playerStats.GetAttributeValue(attributeRegistry.GlobalDamageMultiplier);

        // 3. Calculate the final damage.
        float finalDamage = baseDamage * charMultiplier * globalMultiplier;

        // Find all enemies in a circle around the player
        Collider2D[] enemiesToDamage = Physics2D.OverlapCircleAll(transform.position, ramRadius, enemyLayer);

        Debug.Log($"Ramming Dash triggered! Found {enemiesToDamage.Length} enemies.");

        foreach (var enemyCollider in enemiesToDamage)
        {
            // Try to get the EnemyHealth component from the detected collider
            if (enemyCollider.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.TakeDamage(finalDamage, "player_ram_dash", false, playerStats.playerID);
            }
        }
    }

    // A visual aid to see the ramming radius in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ramRadius);
    }
}