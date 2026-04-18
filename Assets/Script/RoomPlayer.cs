using Fusion;
using System;
using UnityEngine;

[Serializable]
public class RoomPlayer
{
    public string Name;
    public bool IsHost;
    public bool IsReady;

    [NonSerialized]
    public Client Client;
}
