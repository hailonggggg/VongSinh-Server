using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Fusion;
using UnityEngine;

public class BattlePlayer
{
    public string Name { get; }
    public bool IsDeployed = false;
    public bool IsBannedOtherUnit = false;
    public bool IsSceneLoaded { get; private set; }
    public bool DoneSetupDeployment { get; private set; }
    public bool IsGameDataLoaded { get; private set; }
    public bool IsLeftSide { get; private set; }
    public Client Client { get; }
    public ActionPointSystem ApSystem => apSystem;
    public YuanPressureSystem YuanPressureSystem => yuanPressureSystem;
    public IReadOnlyList<int> DeployedUnitIds => deployedUnitIds;
    public IReadOnlyCollection<int> BannedUnitIds => bannedUnitIds;
    public IDictionary<int, Unit> UnitCombats => unitsByCharId;
    public List<SkillTileData> SelectedTileAffectedTargets = new();
    public BattleContext BattleContext;
    public List<Unit> ListUnitHavePendingDamage;

    private readonly List<int> deployedUnitIds = new();
    private readonly HashSet<int> deployedUnitIdSet = new();
    private readonly HashSet<int> bannedUnitIds = new();
    private readonly Dictionary<int, Unit> unitsByCharId = new();
    private readonly ActionPointSystem apSystem = new();
    private readonly YuanPressureSystem yuanPressureSystem = new();


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

    public void HandleBanPickTurnStart(Battle battle, bool hasBanPhase)
    {
        if (hasBanPhase && !IsBannedOtherUnit)
        {
            return;
        }

        if (!IsDeployed)
        {
            ServerNetwork.Instance
                .SendToClients(
                    Service.SendPlayerTurnToDeploy(battle.CurrentTurnCount, Client.PlayerRef.PlayerId),
                    battle.PlayerClients
                );
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

    public void HandleCombatTurnStart(Battle battle)
    {
        int apGainFromAliveUnits = unitsByCharId.Count(x => x.Value.IsAlive);
        apSystem.PlusPoint(apGainFromAliveUnits);
        TriggerAllUnitTurnStartPassive(battle);
        ServerNetwork.Instance.SendToClient(Client, Service.PlayerResourceInfo(apSystem.Current, yuanPressureSystem.Current));
        ServerNetwork.Instance.SendToClients(
            Service.CombatTurnInfo(Client.PlayerRef.PlayerId),
            battle.PlayerClients);
    }

    private void TriggerAllUnitTurnStartPassive(Battle battle)
    {
        foreach (var unit in unitsByCharId.Values)
        {
            if (!unit.IsAlive)
            {
                continue;
            }
            if (unit.Data.IsYuanUser)
            {
                unit.PlusSkillPoint(1);
            }
            unit.TriggerPassives(PassiveTriggerType.TurnStart, new TurnStartEvent(unit), battle.CreateBattleContext(this, unit));
        }
    }

    public void InitializeUnit()
    {
        foreach (var unit in unitsByCharId.Values)
        {
            unit.AddAllSkill();
        }
    }

    public void HandleUnitMove(Battle battle, int unitId, Vector3Int currentCell, Vector3Int targetCell)
    {
        if (!ApSystem.TryConsume(battle.Config.MoveActionCost))
        {
            return;
        }

        if (!unitsByCharId.TryGetValue(unitId, out Unit unit))
        {
            ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Bạn không có nhân vật này trong đội hình."));
            return;
        }

        if (unit.CurrentGridPosition != currentCell)
        {
            return;
        }

        if (!battle.CurrentMap.ContainWalkablePos(targetCell))
        {
            ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Vị trí không hợp lệ."));
            return;
        }
        BattlePlayer opponentPlayer = battle.PlayersById.FirstOrDefault(x => x.Key != Client.PlayerRef.PlayerId).Value;
        List<Unit> units = unitsByCharId.Values
            .Concat(opponentPlayer.UnitCombats.Values)
            .ToList();

        HashSet<Vector3Int> blockCells = battle.CurrentMap.GetOccupiedCells(units);

        if (!battle.CurrentMap.IsWithinMoveRange(unit.CurrentGridPosition, targetCell, unit.MoveRange, blockCells))
        {
            ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Vị trí đến nằm ngoài phạm vi."));
            return;
        }

        List<Vector3Int> paths = battle.CurrentMap.FindPathToTarget(unit.CurrentGridPosition, targetCell, blockCells);
        if (paths == null || paths.Count == 0)
        {
            return;
        }
        unit.CurrentGridPosition = targetCell;
        unit.TriggerPassives(PassiveTriggerType.ActionPerformed, new ActionPerformedEvent(unit), battle.CreateBattleContext(this, unit));
        ServerNetwork.Instance.SendToClient(Client, Service.PlayerResourceInfo(ApSystem.Current, YuanPressureSystem.Current));
        ServerNetwork.Instance.SendToClients(Service.UnitMove(Client.PlayerRef.PlayerId, unit.Id, paths), battle.PlayerClients);
    }

    public void SetUnitPlaced(Battle battle, PlaceUnit placeUnit)
    {
        if (!deployedUnitIds.Contains(placeUnit.UnitId))
        {
            return;
        }

        if (!unitsByCharId.TryGetValue(placeUnit.UnitId, out Unit unit))
        {
            Master.Instance.CharactersById.TryGetValue(placeUnit.UnitId, out Unit character);
            if (character == null)
            {
                ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification($"Đơn vị {placeUnit.UnitId} không tồn tại"));
                return;
            }
            unit = character.Clone();
            unit.CurrentGridPosition = placeUnit.PlacedPosition;
            unit.SetOwner(this, battle);
            unitsByCharId[placeUnit.UnitId] = unit;

            ServerNetwork.Instance.SendToClients(
                Service.PlaceUnitResult(
                unit.Id,
                placeUnit.UnitId,
                IsLeftSide ? battle.CurrentMap.LeftSpawnFacing : battle.CurrentMap.RightSpawnFacing,
                placeUnit.PlacedPosition,
                Client.PlayerRef.PlayerId),
                battle.PlayerClients);
        }

        if (unit == null) return;

        if (!battle.CurrentMap.IsValidSpawnPointPosition(IsLeftSide, placeUnit.PlacedPosition))
        {
            unitsByCharId.Remove(placeUnit.UnitId);
            ServerNetwork.Instance.SendToClients(
                Service.RemoveUnit(placeUnit.UnitId, Client.PlayerRef.PlayerId), battle.PlayerClients);

            return;
        }

        unit.CurrentGridPosition = placeUnit.PlacedPosition;

        ServerNetwork.Instance.SendToClients(
            Service.PlaceUnitResult(
            unit.Id,
            placeUnit.UnitId,
            IsLeftSide ? battle.CurrentMap.LeftSpawnFacing : battle.CurrentMap.RightSpawnFacing,
            placeUnit.PlacedPosition,
            Client.PlayerRef.PlayerId),
            battle.PlayerClients);
    }

    public bool TryHandleUnitDeploySelectedSkill(Battle battle, UnitDeploySelectedSkillRequest request)
    {
        if (!unitsByCharId.TryGetValue(request.CharId, out Unit unit))
        {
            return false;
        }

        SkillLoadoutType loadoutType = (SkillLoadoutType)request.Type;
        if (unit.GetListSkillLoadout(loadoutType).Contains(request.SkillId))
        {
            unit.RemoveSkillEquipped(request.SkillId, loadoutType);
            ServerNetwork.Instance.SendToClients(
                Service.SendUnitDeploySelectedSkill(Client.PlayerRef.PlayerId, unit.Id, request.SkillId, request.Type, false),
                battle.PlayerClients);
            return true;
        }

        if (unit.TryAssignSkill(request.SkillId, loadoutType))
        {
            ServerNetwork.Instance.SendToClients(
               Service.SendUnitDeploySelectedSkill(Client.PlayerRef.PlayerId, unit.Id, request.SkillId, request.Type, true),
               battle.PlayerClients);
            return true;
        }

        return false;
    }

    public void HandeUseSkill(Battle battle, UseSkillRequest request)
    {
        if (!unitsByCharId.TryGetValue(request.UnitId, out Unit unit))
        {
            ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Bạn không có nhân vật này trong đội hình."));
            return;
        }
        Skill selectedSkill = null;
        SkillLoadoutType skillLoadoutType = (SkillLoadoutType)request.SkillType;
        Vector3Int previewDirection = Vector3Int.zero;

        if (skillLoadoutType == SkillLoadoutType.BasicAttack)
        {
            unit.BasicAttackByIds.TryGetValue(request.SkillId, out BasicAttackSkill basicAttack);
            if (basicAttack == null)
            {
                ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Kỹ năng không hợp lệ."));
                return;
            }
            basicAttack.IsYuanMode = yuanPressureSystem.IsYuanMode;
            selectedSkill = basicAttack;
            previewDirection = SkillHandler.GetSkillPreviewDirection(basicAttack.IsDirectional, unit, request.TargetCell);
        }
        else if (skillLoadoutType == SkillLoadoutType.YuanSkill)
        {
            unit.YuanSkillByIds.TryGetValue(request.SkillId, out YuanSkill yuanSkill);
            if (yuanSkill == null)
            {
                ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Kỹ năng không hợp lệ."));
                return;
            }
            selectedSkill = yuanSkill;
            previewDirection = SkillHandler.GetSkillPreviewDirection(yuanSkill.IsDirectional, unit, request.TargetCell);
        }

        if (!ApSystem.TryConsume(selectedSkill.ActionPointCost))
        {
            ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Bạn không đủ AP để sử dụng kỹ năng."));
            return;
        }

        if (selectedSkill.SkillPointCost > 0 && !unit.TryConsumeSkillPoint(selectedSkill.SkillPointCost))
        {
            ServerNetwork.Instance.SendToClient(Client, Service.ShowNotification("Bạn không đủ Nguyện Lực để sử dụng kỹ năng."));
            return;
        }

        List<SkillTileData> selectedTileAffectedTargets = selectedSkill.GetAffectedTileData(previewDirection);
        BattleContext battleContext = battle.CreateBattleContext(this, unit);

        List<Unit> affectedUnits = new();
        foreach (SkillTileData tileData in selectedTileAffectedTargets)
        {
            Vector3Int cell = tileData.offset + request.TargetCell;
            Unit enemy = battleContext.Enemies.FirstOrDefault(x => x.CurrentGridPosition == cell);
            if (enemy == null)
            {
                continue;
            }
            enemy.PendingDamage += (int)(tileData.damageMultiplier * selectedSkill.Damage);
            affectedUnits.Add(enemy);
        }

        ListUnitHavePendingDamage = affectedUnits;
        yuanPressureSystem.AdjustValue(selectedSkill.YuanLiCost);
        unit.TriggerPassives(PassiveTriggerType.ActionPerformed, new ActionPerformedEvent(unit), battle.CreateBattleContext(this, unit));
        ServerNetwork.Instance.SendToClient(Client, Service.YuanPressureUpdate(yuanPressureSystem.Current));
        ServerNetwork.Instance.SendToClients(
           Service.UseSkillResult(
               Client.PlayerRef.PlayerId,
               unit.Id,
               selectedSkill.AnimationTrigger.ToString(),
               request.TargetCell),
               battle.PlayerClients
           );
    }
}

public sealed class PassiveContext
{
    public PassiveTriggerType Trigger;
    public IPassiveEvent PassiveEvent;
    public BattleContext BattleContext;

    public bool TryGetEvent<T>(out T evt) where T : struct, IPassiveEvent
    {
        if (PassiveEvent is T casted)
        {
            evt = casted;
            return true;
        }
        evt = default;
        return false;
    }

}

public interface IPassiveEvent
{

}

public struct TurnStartEvent : IPassiveEvent
{
    public Unit Unit;
    public TurnStartEvent(Unit unit)
    {
        Unit = unit;
    }
}

public struct ActionPerformedEvent : IPassiveEvent
{
    public Unit Actor;
    public ActionPerformedEvent(Unit actor)
    {
        Actor = actor;
    }
}

public struct AttackHitEvent : IPassiveEvent
{
    public Unit Attacker;
    public int EnemyGetHitCount;
    public AttackHitEvent(Unit attacker, int hitCount = 0)
    {
        Attacker = attacker;
        EnemyGetHitCount = hitCount;
    }
}

public struct SkillUsedEvent : IPassiveEvent
{
    public Unit Owner;
    public Skill Skill;
    public SkillUsedEvent(Unit owner, Skill skill)
    {
        Owner = owner;
        Skill = skill;
    }
}
