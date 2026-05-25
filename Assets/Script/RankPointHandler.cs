using System.Collections.Generic;
using UnityEngine;

public static class RankPointHandler
{
    private static int[] pointReceives = new int[3] { 30, 20, 10 };
    public static void UpRankPoint(ref int currentRankPoint, out int currentRank, int rankPointRequireToUpRank)
    {
        currentRank = Mathf.FloorToInt(currentRankPoint / rankPointRequireToUpRank);
        int pointReceive = pointReceives[currentRank];
        currentRankPoint += pointReceive;
        currentRank = Mathf.FloorToInt(currentRankPoint / rankPointRequireToUpRank);
    }
}
