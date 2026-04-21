using Assets.Script;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class Client : IEquatable<Client>
{
    public Player Player;

    public PlayerRef PlayerRef;

    public int CurrentRoomId = -1;

    public int CurrentBattleId = -1;

    public string Token;

    public Queue<Action> PendingPacket = new();

    public Client(PlayerRef playerRef)
    {
        PlayerRef = playerRef;
    }

    public bool Equals(Client other)
    {
        return PlayerRef == other.PlayerRef;
    }

    // public static bool operator ==(Client left, Client right)
    // {
    //     return left.Equals(right);
    // }
    // public static bool operator !=(Client left, Client right)
    // {
    //     return !left.Equals(right);
    // }
}
