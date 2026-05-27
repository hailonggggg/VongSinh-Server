using System;
using UnityEngine;

[Serializable]
public class LeaderboardResponse
{
    public string Message { get; set; }
    public LeaderboardMeta Meta { get; set; }
    public LeaderboardData Data { get; set; }
}
