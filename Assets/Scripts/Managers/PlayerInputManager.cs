// GameClient/Assets/Scripts/Managers/PlayerInputManager.cs
using UnityEngine;

/// <summary>
/// A singleton manager that centralizes player input handling.
/// It holds the single instance of PlayerControls and provides public
/// methods to switch between input action maps (e.g., Player, UI).
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; private set; }

    public PlayerControls PlayerControls { get; private set; }

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
        // Default to player controls when the game starts or manager is enabled.
        SwitchToPlayerControls();
    }

    private void OnDisable()
    {
        // Disable all maps when the manager is disabled to prevent lingering input.
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