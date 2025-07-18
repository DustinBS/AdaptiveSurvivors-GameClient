// GameClient/Assets/Scripts/Player/Behaviors/RammingDash.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// A self-contained behavior that damages enemies and creates a visual afterimage trail
/// when the player dashes. This component is added to the player as a child GameObject
/// when they select the corresponding upgrade.
/// </summary>
public class RammingDash : MonoBehaviour
{
    [Header("Ramming Settings")]
    [Tooltip("Base damage dealt by the ramming attack.")]
    [SerializeField] private float ramDamage = 25f;
    [Tooltip("The radius around the player where enemies will be damaged.")]
    [SerializeField] private float ramRadius = 1.5f;
    [Tooltip("The tag used to identify enemy GameObjects.")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Afterimage Visuals")]
    [Tooltip("The prefab to use for the afterimage effect. This prefab must have the AfterimageFX script attached.")]
    [SerializeField] private GameObject afterimagePrefab;
    [Tooltip("How frequently an afterimage is spawned during the dash (in seconds).")]
    [SerializeField] private float afterimageSpawnRate = 0.05f;

    // --- Component References ---
    private PlayerStats playerStats;
    private PlayerMovement playerMovement;
    private SpriteRenderer playerSpriteRenderer;
    private AttributeRegistry attributeRegistry;

    void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError("RammingDash could not find PlayerStats on any parent GameObject. This behavior will not function.", this);
            this.enabled = false;
            return;
        }

        playerMovement = playerStats.GetComponent<PlayerMovement>();
        playerSpriteRenderer = playerStats.GetComponentInChildren<SpriteRenderer>();

        attributeRegistry = Resources.FindObjectsOfTypeAll<AttributeRegistry>()[0];

        if (playerMovement == null)
        {
            Debug.LogError("RammingDash could not find PlayerMovement on the PlayerStats GameObject.", this);
            this.enabled = false;
        }
        if (playerSpriteRenderer == null)
        {
            Debug.LogError("RammingDash could not find a SpriteRenderer in the children of the PlayerStats GameObject. Afterimages will not work.", this);
        }
    }

    void OnEnable()
    {
        if (playerMovement != null)
        {
            PlayerMovement.OnPlayerDashed += HandleDashStart;
        }
    }

    void OnDisable()
    {
        if (playerMovement != null)
        {
            PlayerMovement.OnPlayerDashed -= HandleDashStart;
        }
    }

    private void HandleDashStart(Vector2 dashDirection)
    {
        ExecuteRamAttack();

        if (afterimagePrefab != null && playerSpriteRenderer != null && playerSpriteRenderer.sprite != null)
        {
            StartCoroutine(AfterimageRoutine());
        }
    }

    private void ExecuteRamAttack()
    {
        // --- Damage Calculation ---
        float baseDamage = ramDamage;
        float charMultiplier = playerStats.GetAttributeValue(attributeRegistry.CharacterDamageMultiplier);
        float globalMultiplier = playerStats.GetAttributeValue(attributeRegistry.GlobalDamageMultiplier);
        float finalDamage = baseDamage * charMultiplier * globalMultiplier;

        // --- Find and Damage Enemies using TAGS ---
        // 1. Get ALL colliders in the radius, regardless of layer.
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(playerStats.transform.position, ramRadius);

        int enemiesHit = 0;

        // 2. Loop through them and check for the correct tag.
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(enemyTag))
            {
                // 3. This is an enemy, so try to apply damage.
                if (hitCollider.TryGetComponent<EnemyHealth>(out var enemyHealth))
                {
                    enemyHealth.TakeDamage(finalDamage, "player_ram_dash", false, playerStats.playerID);
                    enemiesHit++;
                }
            }
        }

        if (enemiesHit > 0)
        {
            Debug.Log($"Ramming Dash triggered! Damaged {enemiesHit} enemies.");
        }
    }

    private IEnumerator AfterimageRoutine()
    {
        float dashEndTime = Time.time + playerMovement.dashDuration;

        while (Time.time < dashEndTime)
        {
            GameObject afterimageInstance = Instantiate(afterimagePrefab, playerStats.transform.position, playerStats.transform.rotation);

            if (afterimageInstance.TryGetComponent<AfterimageFX>(out var fx))
            {
                fx.Initialize(playerSpriteRenderer.sprite);
            }

            yield return new WaitForSeconds(afterimageSpawnRate);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerStats == null) {
             playerStats = GetComponentInParent<PlayerStats>();
             if (playerStats == null) return;
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerStats.transform.position, ramRadius);
    }
}