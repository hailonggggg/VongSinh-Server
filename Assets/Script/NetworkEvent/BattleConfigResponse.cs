using System;
using UnityEngine;

[Serializable]
public class BattleConfigResponse
{
    public int MaxPlayers = 2;
    public int MaxUnitsPerPlayer = 5;
    public int MinUnitsPerPlayer = 1;
    public bool HasBanPhase = false;
    public float TurnTimeLimit = 30f;
    public float DeploymentTime = 60f;
    public int MoveActionCost = 1;
    public int RankPointLimitToUpRank = 100;
}
