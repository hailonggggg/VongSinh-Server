using Fusion;
using System;
using UnityEngine;

[Serializable]
public class RoomPlayer
{
    public int PlayerId;
    public string Name;
    public bool IsHost;
    public bool IsReady;
    public string AvatarUrl;

    [NonSerialized]
    public Client Client;

    public void Reset()
    {
        IsReady = false;
        Client.CurrentBattleId = -1;
    }
}
