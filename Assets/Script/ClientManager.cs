using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientManager
{
    private static readonly Dictionary<PlayerRef, Client> clients = new Dictionary<PlayerRef, Client>();

    public static void AddClient(Client client)
    {
        if (clients.TryAdd(client.PlayerRef, client))
        {
            Debug.Log($"[CLIENT MANAGER] Client {client.PlayerRef.PlayerId} added.");
        }
    }

    public static void RemoveClient(PlayerRef player)
    {
        if (clients.TryGetValue(player, out Client client) && client != null)
        {
            clients.Remove(player);
            Debug.Log($"[CLIENT MANAGER] Client {client.PlayerRef.PlayerId} removed.");
        }
    }

    public static Client TryGetClient(PlayerRef player)
    {
        return clients.TryGetValue(player, out Client client) ? client : null;
    }

}
