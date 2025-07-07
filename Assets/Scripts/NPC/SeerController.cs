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
    private string encounterId; // Use string for GUIDs or other formats
    private SeerResultPayload cachedSeerPayload;
    private bool hasResultBeenReceived = false;
    private bool isEncounterActive = false;

    public void Initialize(string id)
    {
        this.encounterId = id;
        Debug.Log($"Seer Controller initialized for encounter ID: {encounterId}");
    }

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
        // Ignore messages not intended for this specific encounter
        if (payload.encounterId != this.encounterId)
        {
            Debug.Log($"SeerController ignoring stale message for encounter {payload.encounterId}. Current is {this.encounterId}.");
            return;
        }

        Debug.Log($"SeerController received data for encounter {this.encounterId}.");
        cachedSeerPayload = payload;
        hasResultBeenReceived = true;

        // If the UI is already open, update it with the new data immediately.
        if (isEncounterActive && uiInstance != null)
        {
            uiInstance.UpdateWithBargains(cachedSeerPayload.dialogue, cachedSeerPayload.choices);
        }
    }

    public void Interact()
    {
        if (isEncounterActive) return;

        isEncounterActive = true;
        Debug.Log("Player interacted with Seer. Displaying UI immediately.");
        PlayerInputManager.Instance.SwitchToUIControls();

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

        // Subscribe to the UI's event
        uiInstance.OnBargainSelected += HandleBargainSelected;

        // Check if we ALREADY have the data vs. needing to wait
        if (hasResultBeenReceived)
        {
            uiInstance.UpdateWithBargains(cachedSeerPayload.dialogue, cachedSeerPayload.choices);
        }
        else
        {
            uiInstance.DisplayInitialState("The strands of fate are swirling... what choice will you make?");
        }
    }

    private void HandleBargainSelected(BargainChoice choice)
    {
        // choice will be null if the player picks the "no bargain" option
        if (choice != null)
        {
            playerBargainController?.ApplyBargainEffects(choice.buffs);
            playerBargainController?.ApplyBargainEffects(choice.debuffs);
        }

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
        // Always show prompt as long as the encounter isn't already active.
        return !isEncounterActive ? "Consult the Seer" : "";
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Chat;
    }
}