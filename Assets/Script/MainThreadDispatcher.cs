using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    private static readonly Queue<Action> queue = new Queue<Action>();
    private static readonly object lockObj = new object();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        lock (lockObj)
        {
            while (queue.Count > 0)
            {
                Debug.Log($"[DISPATCHER] Executing queued action, remaining={queue.Count}");
                queue.Dequeue().Invoke();
            }
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (lockObj)
        {
            queue.Enqueue(action);
        }
    }
}