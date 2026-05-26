using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattlePlayerInfo
{
    public int PlayerId;
    public string Name;
    public bool IsHost;
    public bool IsReady;
    public string AvatarUrl;
    public List<int> PickedUnitIds = new();
    public List<int> BannedUnitIds = new();
    [NonSerialized]
    public Client Client;
}
