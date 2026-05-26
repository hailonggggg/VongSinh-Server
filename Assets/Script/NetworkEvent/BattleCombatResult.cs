using System;
using UnityEngine;

[Serializable]
public class BattleCombatResult
{
    public int PlayerId;
    public int LastRankPoint;
    public int CurrentRankPoint;
    public bool IsWin;
    public bool IsRank;
    public int RankLimit;
}
