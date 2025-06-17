// GameClient/Assets/Scripts/Managers/PlayerInputManager.cs
using UnityEngine;

/// <summary>
/// A singleton manager that centralizes player input handling.
/// It holds the PlayerControls instance and manages the active action map.
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; private set; }

    public PlayerControls PlayerControls { get; private set; }

    /// <summary>
    /// Returns true if the Player action map is currently enabled.
    /// This serves as a reliable, central source of truth for game state.
    /// </summary>
    public bool IsPlayerControlsEnabled => PlayerControls.Player.enabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            PlayerControls = new PlayerControls();
        }
    }

    private void OnEnable()
    {
        SwitchToPlayerControls();
    }

    private void OnDisable()
    {
        PlayerControls.Player.Disable();
        PlayerControls.UI.Disable();
    }

    /// <summary>
    /// Disables the UI map and enables the Player map.
    /// </summary>
    public void SwitchToPlayerControls()
    {
        PlayerControls.UI.Disable();
        PlayerControls.Player.Enable();
    }

    /// <summary>
    /// Disables the Player map and enables the UI map.
    /// </summary>
    public void SwitchToUIControls()
    {
        PlayerControls.Player.Disable();
        PlayerControls.UI.Enable();
    }
}
