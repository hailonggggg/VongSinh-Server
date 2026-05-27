using System;
using UnityEngine;

[Serializable]
public class PlayerRankDto
{
    public long Rank { get; set; }
    public int UserId { get; set; }
    public string DisplayName { get; set; }
    public string AvatarUrl { get; set; }
    public long RankPoint { get; set; }
}
