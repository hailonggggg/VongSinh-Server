using UnityEngine;

public static class RankPointHandler
{
    private static int GetRankLevel(int currentRankPoint, int rankPointRequireToUpRank, int maxLevel)
    {
        if (rankPointRequireToUpRank <= 0)
            return 0;

        var rankLevel = currentRankPoint / rankPointRequireToUpRank;
        return Mathf.Clamp(rankLevel, 0, maxLevel);
    }

    public static int GetUpRankPoint(int currentRankPoint, int rankPointRequireToUpRank)
    {
        var rankLevel = GetRankLevel(
            currentRankPoint,
            rankPointRequireToUpRank,
            Master.Instance.Config.PointReceives.Length - 1);

        return Master.Instance.Config.PointReceives[rankLevel];
    }

    public static int GetDownRankPoint(int currentRankPoint, int rankPointRequireToUpRank)
    {
        var rankLevel = GetRankLevel(
            currentRankPoint,
            rankPointRequireToUpRank,
            Master.Instance.Config.PointDeductions.Length - 1);

        var pointDeduction = Master.Instance.Config.PointDeductions[rankLevel];

        return -Mathf.Min(pointDeduction, currentRankPoint);
    }
}