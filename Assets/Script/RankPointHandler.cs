using System.Collections.Generic;
using UnityEngine;

public static class RankPointHandler
{


    public static void UpRankPoint(int currentRankPoint, out int currentRank, int rankPointRequireToUpRank, out int newRankPoint)
    {
        currentRank = Mathf.FloorToInt(currentRankPoint / rankPointRequireToUpRank);
        currentRank = Mathf.Clamp(currentRank, 0, Master.Instance.Config.PointDeductions.Length - 1);
        int pointReceive = Master.Instance.Config.PointReceives[currentRank];
        newRankPoint = currentRankPoint + pointReceive;
        currentRank = Mathf.FloorToInt(currentRankPoint / rankPointRequireToUpRank);
    }

    public static void DownRankPoint(int currentRankPoint, out int currentRank, int rankPointRequireToUpRank, out int newRankPoint)
    {
        currentRank = Mathf.FloorToInt(currentRankPoint / rankPointRequireToUpRank);
        currentRank = Mathf.Clamp(currentRank, 0, Master.Instance.Config.PointDeductions.Length - 1);
        int pointDeduction = Master.Instance.Config.PointDeductions[currentRank];
        newRankPoint = Mathf.Max(0, currentRankPoint - pointDeduction);
        currentRank = Mathf.FloorToInt(newRankPoint / rankPointRequireToUpRank);
    }
}
