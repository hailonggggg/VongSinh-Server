using Assets.Script.Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleState
{
    public BattlePhase Phase = BattlePhase.WaitingForPlayers;
    public int CurrentTurn = 0;
    public int CurrentPlayerId = -1;

    public float TimeRemaining = 30f;
    public float MaxDeploymentTime = 30f;
    public float TurnTimeLimit = 60f; 

    public Dictionary<int, List<Vector3Int>> PlayerDeployZones = new Dictionary<int, List<Vector3Int>>();
    public Dictionary<int, List<DeployableUnit>> PlayerAvailableUnits = new Dictionary<int, List<DeployableUnit>>();
    public Dictionary<int, Dictionary<int, Vector3Int>> PlayerDeployedUnits = new Dictionary<int, Dictionary<int, Vector3Int>>();
    public Dictionary<int, bool> PlayerReadyStatus = new Dictionary<int, bool>();
    public int MaxUnitsPerPlayer = 4;
    public int MinUnitsPerPlayer = 1;

    public Dictionary<int, UnitState> Units = new Dictionary<int, UnitState>();
    public Dictionary<Vector3Int, int> OccupiedCells = new Dictionary<Vector3Int, int>(); // Position -> UnitId

    public Dictionary<int, int> PlayerAP = new Dictionary<int, int>();
    public int BaseAPPerTurn = 3;
    public int MaxAP = 6;

    // ==================== PLAYERS ====================
    public List<PlayerState> Players = new List<PlayerState>();

    // ==================== VICTORY ====================
    //public VictoryCondition WinCondition = VictoryCondition.DefeatAllEnemies;
    public int WinnerId = -1;
    public int TotalWaves = 1;
    public int CurrentWave = 1;

    // ==================== HELPERS ====================

    public bool IsDeploymentPhase => Phase == BattlePhase.Deployment;
    public bool IsCombatPhase => Phase == BattlePhase.PlayerTurn || Phase == BattlePhase.EnemyTurn;
    public bool IsFinished => Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat || Phase == BattlePhase.Draw;

    public bool AreAllPlayersReady()
    {
        foreach (var kvp in PlayerReadyStatus)
        {
            if (!kvp.Value) return false;
        }
        return PlayerReadyStatus.Count > 0;
    }

    public int GetDeployedCount(int playerId)
    {
        if (PlayerDeployedUnits.TryGetValue(playerId, out var deployed))
        {
            return deployed.Count;
        }
        return 0;
    }

    public BattleState Clone()
    {
        var json = JsonUtility.ToJson(this);
        return JsonUtility.FromJson<BattleState>(json);
    }
}

[Serializable]
public class UnitState
{
    public int UnitId;
    public int OwnerId;
    public string UnitName;
    //public UnitType Type;
    public Vector3Int Position;

    // Stats
    public int CurrentHP;
    public int MaxHP;
    public int Attack;
    public int Defense;
    public int MoveRange = 3;
    public int AttackRange = 1;

    // Status
    public bool IsFatigued;
    public int ActionsThisTurn;
    public List<string> StatusEffects = new List<string>();

    public bool IsAlive => CurrentHP > 0;
}

[Serializable]
public class PlayerState
{
    public int PlayerId;
    public string PlayerName;
    public bool IsReady;
    public bool IsConnected;
    public bool IsAI; // For PvE enemy
    public List<int> UnitIds = new List<int>();

    public bool IsDefeated => UnitIds.Count == 0;
}

[Serializable]
public class DeployableUnit
{
    public int UnitId;
    public string UnitName;
    //public UnitType Type;
    public Sprite Portrait;
    public GameObject Prefab;
    public bool IsDeployed;

    // Preview stats
    public int MaxHP;
    public int Attack;
    public int Defense;
    public int MoveRange;
    public int AttackRange;
}