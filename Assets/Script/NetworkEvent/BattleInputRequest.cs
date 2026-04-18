using Assets.Script.Shared;
using System;

[Serializable]
public class BattleInputRequest
{
    public string BattleId;
    public int ClientSequence;
    public CommandType CommandType;
    public int UnitId;
    public int TargetUnitId;
    public int TargetX;
    public int TargetY;
    public int TargetZ;
    public bool Ready;
}
