using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;

public class BattlePlayer
{
    public bool IsDeployed = false;
    public bool IsBannedOtherUnit = false;
    public string Name { get; }
    public bool IsSceneLoaded { get; private set; }
    public bool IsLeftSide { get; private set; }
    public PlayerRef PlayerRef { get; }
    public HashSet<int> DeployedUnitIds = new();
    public HashSet<int> BannedUnitIds = new();
    public HashSet<Unit> UnitCombats = new();

    public BattlePlayer(PlayerRef playerRef, string name, bool isLeftSide)
    {
        PlayerRef = playerRef;
        Name = name;
        IsLeftSide = isLeftSide;
    }

    public bool MarkSceneLoaded()
    {
        if (IsSceneLoaded)
        {
            return false;
        }

        IsSceneLoaded = true;
        return true;
    }

    public bool ApplyUnitDeploy(int unitDeployId)
    {
        return DeployedUnitIds.Add(unitDeployId);
    }

    public bool ApplyUnitBan(int unitBanId)
    {
        return BannedUnitIds.Add(unitBanId);
    }

    public void AddUnit(Unit unit)
    {
        UnitCombats.Add(unit);
    }

    public bool HasReachedDeployLimit(int length)
    {
        return DeployedUnitIds.Count >= length;
    }

    public void HandleTurnStart(Battle battle, bool hasBanPhase)
    {
        if (hasBanPhase && !IsBannedOtherUnit)
        {
            // Wait for player to select a unit to ban
            return;
        }
        if (!IsDeployed)
        {
            ServerNetwork.Instance.SendToClients(Service.SendPlayerTurnToDeploy(battle.CurrentTurnCount, Name), battle.Players.Select(x => x.PlayerRef).ToArray());
            IsDeployed = true;
            return;
        }
        battle.HandlePlayerTurnDone();
    }

    public void ResetState()
    {
        IsDeployed = false;
        IsBannedOtherUnit = false;
    }
}
