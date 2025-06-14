// GameClient/Assets/Scripts/Managers/GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Management")]
    [Tooltip("The player prefab to instantiate if not already in the scene.")]
    public GameObject playerPrefab;

    [Header("Game State")]
    [Tooltip("The current wave number of the game.")]
    public int currentWave = 0; // [FIX] Added property for wave tracking

    private PlayerStatus playerStatus;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        playerStatus = FindObjectOfType<PlayerStatus>();

        if (playerStatus == null)
        {
            if (playerPrefab != null)
            {
                var playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                playerStatus = playerInstance.GetComponent<PlayerStatus>();
            }
        }

        if (playerStatus != null)
        {
            playerStatus.OnPlayerDeath += HandlePlayerDeath;
        }
        else
        {
            Debug.LogError("GameManager: Could not find PlayerStatus component in the scene!", this);
        }
    }

    private void OnDisable()
    {
        if (playerStatus != null)
        {
            playerStatus.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("GameManager received player death event. Handling game over logic.");
        Invoke(nameof(ReloadScene), 3f);
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
