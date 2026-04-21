using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattlePlayerInfo
{
    public string Name;
    public List<int> DeployedUnitIds = new();
    public List<int> BannedUnitIds = new();
}
