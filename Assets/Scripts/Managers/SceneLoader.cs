// GameClient/Assets/Scripts/Managers/SceneLoader.cs

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A simple static class to handle loading different game scenes.
/// Using a dedicated class for this keeps scene management logic clean and centralized.
/// </summary>
public static class SceneLoader
{
    // It's good practice to store scene names or indices in constants
    // to avoid magic strings and make maintenance easier.
    private const string BATTLE_SCENE_NAME = "Battle";
    private const string HUB_SCENE_NAME = "Hub";     // Change this for your hub scene

    /// <summary>
    /// Loads the main battle/gameplay scene.
    /// </summary>
    public static void LoadBattleScene()
    {
        // Before loading, ensure the game state is reset for a fresh run.
        Time.timeScale = 1f; // In case the game was paused (legacy, but good practice)
        SceneManager.LoadScene(BATTLE_SCENE_NAME);
    }

    /// <summary>
    /// Loads the main hub world scene.
    /// </summary>
    public static void LoadHubScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(HUB_SCENE_NAME);
    }

    /// <summary>
    /// A generic method to load any scene by name.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public static void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
