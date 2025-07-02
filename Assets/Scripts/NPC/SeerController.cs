// GameClient/Assets/Scripts/NPC/SeerController.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Orchestrates the Ethereal Seer encounter. It listens for the Seer result from Kafka,
/// controls the game state, manages the Seer's UI, and applies the chosen bargain.
/// </summary>
public class SeerController : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Reference to the Seer's UI controller in the scene.")]
    [SerializeField] private SeerUIController uiController;
    [Tooltip("Reference to the player's bargain controller.")]
    [SerializeField] private PlayerBargainController playerBargainController;

    [Header("Game Object References")]
    [Tooltip("The parent GameObject containing the Seer's visuals, to be enabled/disabled.")]
    [SerializeField] private GameObject seerVisuals;

    void Awake()
    {
        // Ensure components are assigned to prevent null reference errors.
        if (uiController == null) Debug.LogError("SeerUIController not assigned in SeerController.", this);
        if (playerBargainController == null) Debug.LogError("PlayerBargainController not assigned in SeerController.", this);

        // Hide the Seer and its UI by default.
        seerVisuals.SetActive(false);
        uiController.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // Subscribe to the Kafka event when this controller is active.
        KafkaClient.OnSeerResultReceived += HandleSeerResult;
        // Subscribe to the UI event for when the player makes a choice.
        if (uiController != null)
        {
            uiController.OnBargainSelected += HandleBargainSelected;
        }
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks.
        KafkaClient.OnSeerResultReceived -= HandleSeerResult;
        if (uiController != null)
        {
            uiController.OnBargainSelected -= HandleBargainSelected;
        }
    }

    /// <summary>
    /// This is the entry point for the encounter, triggered by a Kafka message.
    /// </summary>
    private void HandleSeerResult(SeerResultPayload payload)
    {
        // Pause the game's wave progression.
        GameManager.Instance.EnterSeerEncounterState();

        // Show the Seer's visuals and UI.
        seerVisuals.SetActive(true);
        uiController.gameObject.SetActive(true);

        // Populate the UI with the data received from the backend.
        uiController.DisplayEncounter(payload.dialogue, payload.choices);
    }

    /// <summary>
    /// This is the exit point for the encounter, triggered by the player's choice in the UI.
    /// </summary>
    private void HandleBargainSelected(BargainChoice choice)
    {
        // Apply the chosen effects to the player.
        if (playerBargainController != null)
        {
            // The SeerController's job is to call methods on other scripts to apply the bargain.
            playerBargainController.ApplyBargainEffects(choice.buffs);
            playerBargainController.ApplyBargainEffects(choice.debuffs);
        }

        // End the encounter.
        EndEncounter();
    }

    private void EndEncounter()
    {
        // Hide the Seer's visuals and UI.
        seerVisuals.SetActive(false);
        uiController.gameObject.SetActive(false);

        // Resume the game's wave progression.
        GameManager.Instance.ExitSeerEncounterState();
    }
}