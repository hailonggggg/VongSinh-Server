using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerBanPickInfo
{
    public string Name;
    public List<int> PickedUnitIds = new();
    public List<int> BannedUnitIds = new();
}
