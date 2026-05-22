using Assets.Script;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class Client : IEquatable<Client>
{
    public UserApiUserData User;
    public PlayerRef PlayerRef;
    public List<UserItem> UserItems;
    public HashSet<int> OwnedCharacterIds = new();

    public int CurrentRoomId = -1;

    public int CurrentBattleId = -1;

    public string Token;

    public Queue<Action> PendingPacket = new();

    public Client(NetworkRunner networkRunner, PlayerRef playerRef)
    {
        PlayerRef = playerRef;
    }

    public bool Equals(Client other)
    {
        return PlayerRef == other.PlayerRef;
    }
}
