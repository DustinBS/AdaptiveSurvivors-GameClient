// GameClient/Assets/Scripts/Managers/PersistentManagers.cs

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The core of the bootstrapper pattern, updated to allow starting the game from any scene.
/// This script uses a RuntimeInitializeOnLoadMethod to ensure that the essential global managers
/// (like KafkaClient) are loaded from a prefab before any scene officially starts.
/// </summary>
public class PersistentManagers : MonoBehaviour
{
    // A static flag to ensure the bootstrapper logic only ever runs once per game session.
    private static bool hasBeenInitialized = false;

    // The path to the PersistentManagers prefab within a "Resources" folder.
    private const string PERSISTENT_MANAGERS_PREFAB_PATH = "PersistentManagers";

    /// <summary>
    /// This method is decorated with a special Unity attribute that ensures it is called
    /// automatically when the game starts in the editor, BEFORE the first scene loads.
    /// This is the key to allowing "play from any scene" functionality.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeManagers()
    {
        // If the managers have not been initialized yet in this session...
        if (!hasBeenInitialized)
        {
            // Load the "PersistentManagers" prefab from any "Resources" folder.
            var persistentManagersPrefab = Resources.Load<GameObject>(PERSISTENT_MANAGERS_PREFAB_PATH);

            if (persistentManagersPrefab == null)
            {
                Debug.LogError("FATAL ERROR: Could not find 'PersistentManagers' prefab in a Resources folder. Please create it.");
                return;
            }

            // Instantiate the prefab to bring the managers into the game.
            var persistentManagersInstance = Instantiate(persistentManagersPrefab);

            // Set the name for clarity in the hierarchy.
            persistentManagersInstance.name = "_PersistentManagers (Runtime)";

            // The Awake method on the instantiated prefab will handle the rest.
        }
    }


    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// We use it here to ensure this object persists across scenes.
    /// </summary>
    void Awake()
    {
        // This script runs on the instance created by the InitializeManagers method.
        if (!hasBeenInitialized)
        {
            // Mark this GameObject to not be destroyed when loading new scenes.
            // All of its children and components will also persist.
            DontDestroyOnLoad(gameObject);

            // Set the flag to true so this logic never runs again for this session.
            hasBeenInitialized = true;
        }
        else
        {
            // If another instance of this prefab somehow gets created, destroy it to enforce the singleton pattern.
            Destroy(gameObject);
        }
    }
}
