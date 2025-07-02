// GameClient/Assets/Scripts/NPC/SeerController.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Orchestrates the Ethereal Seer encounter. It is instantiated by the GameManager,
/// manages its own lifecycle, instantiates its own UI from a prefab, and controls player input state.
/// </summary>
public class SeerController : MonoBehaviour, IInteractable
{
    [Header("Asset References")]
    [Tooltip("Reference to the Seer UI Prefab that will be instantiated.")]
    [SerializeField] private GameObject seerUiPrefab;

    // --- Runtime References ---
    private PlayerBargainController playerBargainController;
    private SeerUIController uiInstance; // A reference to the UI we instantiate.

    // --- State ---
    private SeerResultPayload cachedSeerPayload;
    private bool isReadyForInteraction = false;
    private bool isEncounterActive = false;

    void Start()
    {
        // On start, the Seer has been spawned. We find necessary scene components.
        playerBargainController = FindFirstObjectByType<PlayerBargainController>();
        if (playerBargainController == null)
        {
             Debug.LogError("SeerController could not find a PlayerBargainController in the scene.", this);
             enabled = false;
        }

        // Push the player out of the way if they are in the center.
        if (Vector3.Distance(transform.position, playerBargainController.transform.position) < 1.0f)
        {
            playerBargainController.transform.position += Vector3.up * 1.5f;
        }
    }

    void OnEnable()
    {
        KafkaClient.OnSeerResultReceived += HandleSeerResult;
    }

    void OnDisable()
    {
        KafkaClient.OnSeerResultReceived -= HandleSeerResult;
        // Unsubscribe from the UI instance if it exists
        if (uiInstance != null)
        {
            uiInstance.OnBargainSelected -= HandleBargainSelected;
        }
    }

    private void HandleSeerResult(SeerResultPayload payload)
    {
        Debug.Log("SeerController received data from Kafka. Caching payload and awaiting interaction.");
        cachedSeerPayload = payload;
        isReadyForInteraction = true;
    }

    public void Interact()
    {
        if (!isReadyForInteraction || isEncounterActive) return;

        isEncounterActive = true;
        Debug.Log("Player interacted with Seer. Switching to UI controls and displaying bargain UI.");

        // 1. Switch player input to the UI map for consistency.
        PlayerInputManager.Instance.SwitchToUIControls();

        // 2. Instantiate the UI from the prefab.
        if (seerUiPrefab == null)
        {
            Debug.LogError("Seer UI Prefab is not assigned in SeerController!");
            EndEncounterAbruptly();
            return;
        }
        GameObject uiObject = Instantiate(seerUiPrefab);
        uiInstance = uiObject.GetComponent<SeerUIController>();

        if (uiInstance == null)
        {
            Debug.LogError("Instantiated Seer UI Prefab is missing a SeerUIController component!");
            EndEncounterAbruptly();
            return;
        }

        // 3. Subscribe to the UI's event and populate it with data.
        uiInstance.OnBargainSelected += HandleBargainSelected;
        uiInstance.DisplayEncounter(cachedSeerPayload.dialogue, cachedSeerPayload.choices);
    }

    private void HandleBargainSelected(BargainChoice choice)
    {
        playerBargainController?.ApplyBargainEffects(choice.buffs);
        playerBargainController?.ApplyBargainEffects(choice.debuffs);

        StartCoroutine(DespawnRoutine());
    }

    private void EndEncounterAbruptly()
    {
        // Fallback method in case of error.
        PlayerInputManager.Instance.SwitchToPlayerControls();
        GameManager.Instance.ExitSeerEncounterState();
        Destroy(gameObject);
    }

    private IEnumerator DespawnRoutine()
    {
        isReadyForInteraction = false;

        // Destroy the UI instance immediately.
        if (uiInstance != null)
        {
            uiInstance.OnBargainSelected -= HandleBargainSelected;
            Destroy(uiInstance.gameObject);
        }

        // Play a despawn animation/effect on the Seer itself.
        Debug.Log("Seer is despawning...");
        // TODO: Trigger particle effect or animation here.
        yield return new WaitForSeconds(1.5f);

        // Restore player controls and game state.
        PlayerInputManager.Instance.SwitchToPlayerControls();
        GameManager.Instance.ExitSeerEncounterState();

        // The Seer's final act is to destroy its own GameObject.
        Destroy(gameObject);
    }

    public string GetInteractionPrompt()
    {
        // Only show a prompt if the data has arrived from the backend and the UI isn't already open.
        return (isReadyForInteraction && !isEncounterActive) ? "Consult the Seer" : "";
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Chat;
    }
}