using Assets.Script.Shared;
using System;

[Serializable]
public class BattleSnapshotMessage
{
    public string BattleId;
    public int ServerTick;
    public int StateHash;
    public BattlePhase Phase;
    public int CurrentTurn;
    public int CurrentPlayerId;
    public float TimeRemaining;
    public float MaxDeploymentTime;
    public float TurnTimeLimit;
    public int WinnerId;
    public int CurrentWave;
    public int TotalWaves;
    public BattlePlayerReadyEntry[] PlayerReady;
    public BattleUnitPositionEntry[] OccupiedCells;
    public BattlePlayerAckEntry[] LastProcessedSequences;
}

[Serializable]
public class BattlePlayerReadyEntry
{
    public int PlayerId;
    public bool IsReady;
}

[Serializable]
public class BattleUnitPositionEntry
{
    public int UnitId;
    public int X;
    public int Y;
    public int Z;
}

[Serializable]
public class BattlePlayerAckEntry
{
    public int PlayerId;
    public int LastProcessedSequence;
}
