using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchmakingPlayer
{
    public Client Client { get; set; }
    public DateTime JoinTime { get; set; }

    public float WaitTime => (float)(DateTime.UtcNow - JoinTime).TotalSeconds;
}

public static class MatchmakingQueue
{
    private static readonly List<MatchmakingPlayer> queue = new();

    public static void AddPlayer(Client client)
    {
        if (queue.Any(p => p.Client == client)) return;

        queue.Add(new MatchmakingPlayer
        {
            Client = client,
            JoinTime = DateTime.UtcNow
        });
        Debug.Log($"[Matchmaking] Player {client.User.LastName} joined the queue.");
    }

    public static void RemovePlayer(Client client)
    {
        var player = queue.FirstOrDefault(p => p.Client == client);
        if (player != null)
        {
            queue.Remove(player);
            Debug.Log($"[Matchmaking] Player {client.User.LastName} left the queue.");
        }
    }

    public static List<MatchmakingPlayer> GetQueue() => queue;

    public static List<MatchmakingPlayer> GetPair()
    {
        if (queue.Count < 2) return null;

        // Lấy 2 người chờ lâu nhất
        var pair = queue.Take(2).ToList();
        return pair;
    }

    public static void RemovePair(List<MatchmakingPlayer> pair)
    {
        if (pair == null) return;
        foreach (var p in pair)
        {
            queue.Remove(p);
        }
    }
}
