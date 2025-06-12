// GameClient/Assets/Scripts/Utilities/UnityMainThreadDispatcher.cs

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// A helper to dispatch actions to the Unity main thread from background threads.
/// This component's persistence is managed by its parent GameObject (_PersistentManagers).
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    // A thread-safe queue for actions that need to run on the main thread.
    private readonly Queue<Action> executionQueue = new Queue<Action>();

    // This is no longer a full singleton but is still easily accessible.
    // It assumes it will be available on the persistent manager object.
    public static UnityMainThreadDispatcher Instance { get; private set; }

    void Awake()
    {
        // Simple singleton assignment. The PersistentManagers script ensures only one instance exists.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Lock the queue to prevent other threads from modifying it while we process it.
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                // Dequeue and invoke the action on the main thread.
                executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// This method is thread-safe.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    public void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
}
