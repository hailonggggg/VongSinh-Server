using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RankPlayerInfo
{
    public int Rank;
    public string PlayerName;
    public string PlayerAvatarUrl;
    public int Score;
    public int Wins;
    public int Losses;
}

[Serializable]
public class RankList
{
    public List<RankPlayerInfo> Players;
}