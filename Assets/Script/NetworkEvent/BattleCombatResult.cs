using System;
using UnityEngine;

[Serializable]
public class BattleCombatResult
{
    public int PlayerId;
    public int NewRankPoint;
    public int CurrentRank;
    public bool IsWin;
    public int RankLimit;
}
