using System.Collections.Generic;
using System.Linq;
using Fusion;

public class BattlePlayer
{
    public bool IsDeployed = false;
    public bool IsBannedOtherUnit = false;
    public string Name { get; }
    public bool IsSceneLoaded { get; private set; }
    public bool DoneSetupDeployment { get; private set; }
    public bool IsGameDataLoaded { get; private set; }
    public bool IsLeftSide { get; private set; }
    public Client Client { get; }
    public IReadOnlyList<int> DeployedUnitIds => deployedUnitIds;
    public IReadOnlyCollection<int> BannedUnitIds => bannedUnitIds;
    public IDictionary<int, Unit> UnitCombats => unitsByCharId;

    private readonly List<int> deployedUnitIds = new();
    private readonly HashSet<int> deployedUnitIdSet = new();
    private readonly HashSet<int> bannedUnitIds = new();
    private readonly Dictionary<int, Unit> unitsByCharId = new();

    public BattlePlayer(Client client, string name, bool isLeftSide)
    {
        Client = client;
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

    public bool MarkGameDataLoaded()
    {
        if (IsGameDataLoaded)
        {
            return false;
        }

        IsGameDataLoaded = true;
        return true;
    }

    public bool MarkSetupDeploymentDone()
    {
        if (DoneSetupDeployment)
        {
            return false;
        }
        DoneSetupDeployment = true;
        return true;
    }

    public bool ApplyUnitDeploy(int unitDeployId)
    {
        if (!deployedUnitIdSet.Add(unitDeployId))
        {
            return false;
        }

        deployedUnitIds.Add(unitDeployId);
        return true;
    }

    public bool ApplyUnitBan(int unitBanId)
    {
        return bannedUnitIds.Add(unitBanId);
    }

    public void AddUnit(Unit unit)
    {
        if (unit == null)
        {
            return;
        }

        unitsByCharId[unit.Id] = unit;
    }

    public bool RemoveUnit(int unitId)
    {
        return unitsByCharId.Remove(unitId);
    }

    public bool TryGetUnit(int unitId, out Unit unit)
    {
        return unitsByCharId.TryGetValue(unitId, out unit);
    }

    public bool TryGetDeployedUnitIdAt(int index, out int unitId)
    {
        unitId = default;
        if (index < 0 || index >= deployedUnitIds.Count)
        {
            return false;
        }

        unitId = deployedUnitIds[index];
        return true;
    }

    public bool HasReachedDeployLimit(int length)
    {
        return deployedUnitIds.Count >= length;
    }

    public void HandleTurnStart(Battle battle, bool hasBanPhase)
    {
        if (hasBanPhase && !IsBannedOtherUnit)
        {
            return;
        }

        if (!IsDeployed)
        {
            ServerNetwork.Instance.SendToClients(
                Service.SendPlayerTurnToDeploy(battle.CurrentTurnCount, Name),
                battle.PlayerClients);
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
    public void ResetSceneLoaded()
    {
        IsSceneLoaded = false;
    }
}
