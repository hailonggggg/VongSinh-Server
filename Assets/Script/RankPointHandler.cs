using System.Collections.Generic;
using UnityEngine;

public static class RankPointHandler
{


    public static void UpRankPoint(int currentRankPoint, int rankPointRequireToUpRank, out int rankPointPlus)
    {
        var currentRankLevel = Mathf.FloorToInt(currentRankPoint / rankPointRequireToUpRank);
        currentRankLevel = Mathf.Clamp(currentRankLevel, 0, Master.Instance.Config.PointReceives.Length - 1);
        rankPointPlus = Master.Instance.Config.PointReceives[currentRankLevel];
    }

    public static void DownRankPoint(int currentRankPoint, int rankPointRequireToUpRank, out int rankPointPlus)
    {
        var currentRankLevel = Mathf.FloorToInt(currentRankPoint / rankPointRequireToUpRank);
        currentRankLevel = Mathf.Clamp(currentRankLevel, 0, Master.Instance.Config.PointDeductions.Length - 1);
        rankPointPlus = Mathf.Max(0, -Master.Instance.Config.PointDeductions[currentRankLevel]);
    }
}
