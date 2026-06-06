using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class BattleConfig
{
    public int MaxPlayers = 2;
    public int MaxUnitsPerPlayer = 5;
    public int MinUnitsPerPlayer = 1;
    public bool HasBanPhase = false;
    public float TurnTimeLimit = 30f;
    public float DeploymentTime = 60f;
    public int MoveActionCost = 1;
    public int[] AllowMapIds = new int[] { 1 };
    public int[] ListUnitIdHasData = new int[] { 1, 2 };
    public int RankPointLimitToUpRank = 100;
    public int[] PointReceives = new int[3] { 30, 20, 10 };
    public int[] PointDeductions = new int[3] { 10, 15, 20 };
}


