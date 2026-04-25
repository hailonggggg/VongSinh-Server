using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BanSystem
{
    private static Dictionary<string, BanEntry> bannedUsers = new();
    private static HashSet<string> bannedIPs = new();

    public static void HandlePlayerJoin(NetworkRunner runner, PlayerRef player)
    {
        string userId = GetUserId(runner, player);
        string ip = GetPlayerIP(player);

        if (IsUserBanned(userId))
        {
            Debug.Log($"[BAN] User {userId} rejected");
            runner.Disconnect(player);
            return;
        }

        if (bannedIPs.Contains(ip))
        {
            Debug.Log($"[BAN] IP {ip} rejected");
            runner.Disconnect(player);
            return;
        }

        ClientManager.AddClient(new Client(runner, player));
        Debug.Log($"Player accepted: {userId}");
    }

    // -------- BAN CHECK --------

    public static bool IsUserBanned(string userId)
    {
        if (!bannedUsers.ContainsKey(userId)) return false;

        var entry = bannedUsers[userId];

        if (entry.ExpireAt.HasValue && DateTime.UtcNow > entry.ExpireAt.Value)
        {
            bannedUsers.Remove(userId);
            return false;
        }

        return true;
    }

    // -------- BAN ACTION --------

    public static void BanUser(string userId, int durationMinutes, string reason)
    {
        DateTime? expire = durationMinutes > 0
            ? DateTime.UtcNow.AddMinutes(durationMinutes)
            : null;

        bannedUsers[userId] = new BanEntry
        {
            UserId = userId,
            Reason = reason,
            ExpireAt = expire
        };

        Debug.Log($"[BAN] {userId} - {reason}");
    }

    public static void BanIP(string ip)
    {
        bannedIPs.Add(ip);
        Debug.Log($"[BAN IP] {ip}");
    }

    public static void Kick(NetworkRunner runner, PlayerRef player)
    {
        runner.Disconnect(player);
    }

    static string GetUserId(NetworkRunner runner, PlayerRef player)
    {
        return runner.GetPlayerUserId(player);
    }

    static string GetPlayerIP(PlayerRef player)
    {
        return "UNKNOWN_IP";
    }
}

public class BanEntry
{
    public string UserId;
    public string Reason;
    public DateTime? ExpireAt;
}