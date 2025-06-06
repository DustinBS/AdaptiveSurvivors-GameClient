// GameClient/Assets/Scripts/Environment/BreakableObject.cs

using UnityEngine;
using System.Collections.Generic; // For Dictionary

// This script defines a breakable environmental object.
// It can take damage and, upon "destruction," sends a breakable_object_destroyed_event to Kafka.
// It assumes the object has a Collider2D and optionally a Rigidbody2D.

public class BreakableObject : MonoBehaviour
{
    [Header("Object Settings")]
    [Tooltip("Unique identifier for this object instance.")]
    public string objId = "object_001";

    [Tooltip("The type of this object (e.g., 'barrel', 'crate', 'tombstone', 'bush').")]
    public string objType = "barrel";

    [Tooltip("The health points of this breakable object. Set to 1 if it's a single-hit object.")]
    public float currentHealth;

    [Tooltip("The maximum health points of this breakable object.")]
    public float maxHealth = 1f; // Default to 1 for one-hit destruction

    // Reference to the KafkaClient instance in the scene
    private KafkaClient kafkaClient;

    // Player ID to associate with Kafka events (who destroyed it)
    [Tooltip("ID of the player interacting with this object (usually the main player).")]
    public string playerId = "player_001"; // Should match the main player's ID

    void Awake()
    {
        currentHealth = maxHealth;

        // Using FindAnyObjectByType to resolve deprecation warning
        kafkaClient = FindAnyObjectByType<KafkaClient>();
        if (kafkaClient == null)
        {
            Debug.LogError("BreakableObject: KafkaClient not found in the scene. Please add a GameObject with KafkaClient.cs.", this);
            enabled = false;
        }

        // Assign a unique ID if not set in inspector
        if (string.IsNullOrEmpty(objId) || objId == "object_001")
        {
            objId = objType + "_" + GetInstanceID(); // Simple unique ID
        }
    }

    /// <summary>
    /// Reduces the object's health.
    /// This method can be called by player attacks or other damaging sources.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    /// <param name="weaponId">The ID of the weapon that dealt the damage.</param>
    public void TakeDamage(float amount, string weaponId)
    {
        if (!enabled) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} ({objId}) took {amount} damage. Current Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            DestroyObject(weaponId);
        }
    }

    /// <summary>
    /// Handles the destruction of the object.
    /// </summary>
    /// <param name="weaponId">The ID of the weapon that destroyed the object.</param>
    private void DestroyObject(string weaponId)
    {
        Debug.Log($"{gameObject.name} ({objId}) was destroyed by {weaponId}!");

        // Send breakable_object_destroyed_event to Kafka
        SendBreakableObjectDestroyedEvent(weaponId);

        // In a real game, play destruction animation, spawn pickups, etc.
        Destroy(gameObject);
    }

    /// <summary>
    /// Sends a breakable_object_destroyed_event to Kafka.
    /// </summary>
    /// <param name="weaponId">The ID of the weapon that destroyed the object.</param>
    private void SendBreakableObjectDestroyedEvent(string weaponId)
    {
        var payload = new Dictionary<string, object>
        {
            { "obj_id", objId },
            { "obj_type", objType },
            { "weapon_id", weaponId }
        };
        kafkaClient.SendGameplayEvent("breakable_object_destroyed_event", playerId, payload);
    }

    // Example of how to interact with this object from PlayerAttack or a projectile:
    // When a player projectile hits something with a Collider2D:
    // void OnTriggerEnter2D(Collider2D other)
    // {
    //     BreakableObject breakable = other.GetComponent<BreakableObject>();
    //     if (breakable != null)
    //     {
    //         breakable.TakeDamage(damageAmount, "player_projectile_id");
    //     }
    // }
}
