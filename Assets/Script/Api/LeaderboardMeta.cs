using System;
using UnityEngine;

[Serializable]
public class LeaderboardMeta
{
    public string Direction { get; set; }
    public bool HasBefore { get; set; }
    public bool HasAfter { get; set; }
    public long TopRank { get; set; }
    public long BottomRank { get; set; }
}
