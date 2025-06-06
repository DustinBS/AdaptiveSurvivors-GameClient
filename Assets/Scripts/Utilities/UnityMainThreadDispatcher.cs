// --- UnityMainThreadDispatcher ---
// Assets/Scripts/Utilities/UnityMainThreadDispatcher.cs
// A simple helper to dispatch actions to the Unity main thread from background threads.
// This is necessary because most Unity API calls (e.g., Debug.Log, GameObject.Instantiate)
// must be made from the main thread.
using UnityEngine;
using System;

// NOTE: This UnityMainThreadDispatcher is now updated to use FindAnyObjectByType to resolve the deprecation warning.
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly System.Collections.Generic.Queue<Action> _executionQueue = new System.Collections.Generic.Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            // Using FindAnyObjectByType to resolve the deprecation warning
            _instance = FindAnyObjectByType<UnityMainThreadDispatcher>();
            if (_instance == null)
            {
                GameObject obj = new GameObject("UnityMainThreadDispatcher");
                _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
        }
        return _instance;
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
