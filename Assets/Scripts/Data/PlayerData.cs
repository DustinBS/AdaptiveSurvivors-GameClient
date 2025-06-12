// GameClient/Assets/Scripts/Data/PlayerData.cs

using UnityEngine;

/// <summary>
/// A ScriptableObject that holds static data for the player character,
/// such as their name and portrait for use in UI systems like dialogue.
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Adaptive Survivors/Player Data")]
public class PlayerData : ScriptableObject
{
    [Tooltip("The player's name, to be displayed in dialogue.")]
    public string playerName = "Survivor";

    [Tooltip("The player's portrait sprite for the dialogue UI.")]
    public Sprite playerPortrait;
}
