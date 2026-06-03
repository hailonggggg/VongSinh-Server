using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Fusion;
using UnityEngine;

public class BattlePlayer
{
    private const int MaxDeployUnit = 5;
    private const int MaxBanUnit = 5;
    private const int MaxUnitBanPerTurn = 2;
    public string Name { get; }
    public bool IsDeployed = false;

    public bool IsSceneLoaded { get; private set; }
    public bool DoneSetupDeployment { get; private set; }
    public bool IsGameDataLoaded { get; private set; }
    public bool IsLeftSide { get; private set; }
    public Client Client { get; }
    public bool IsTeamEliminated => unitsByCharId.Values.All(u => !u.IsAlive);

    #region Banpick
    public IReadOnlyCollection<int> PickedUnitIds => pickedUnitIds;
    public IReadOnlyCollection<int> BannedUnitIds => bannedUnitIds;
    public IReadOnlyCollection<int> OwnerUnlockedUnitId => ownerUnlockedUnitId;
    private readonly HashSet<int> bannedUnitIds = new(MaxBanUnit);
    private readonly HashSet<int> pickedUnitIds = new(MaxDeployUnit);
    private readonly HashSet<int> ownerUnlockedUnitId = new();

    #endregion

    #region Combat
    public IDictionary<int, Unit> UnitCombats => unitsByCharId;
    public ActionPointSystem ApSystem => apSystem;
    public YuanPressureSystem YuanPressureSystem => yuanPressureSystem;
    public List<SkillTileData> SelectedTileAffectedTargets => selectedTileAffectedTargets;
    public List<Unit> ListUnitHavePendingDamage => listUnitHavePendingDamage;
    private readonly List<Unit> listUnitHavePendingDamage = new();
    private readonly Dictionary<int, Unit> unitsByCharId = new();
    private readonly List<SkillTileData> selectedTileAffectedTargets = new();
    private readonly ActionPointSystem apSystem = new();
    private readonly YuanPressureSystem yuanPressureSystem = new();
    private Unit lastUnitUsedSkill;
    private Skill selectedSkill;

    #endregion


    public BattlePlayer(Client client, string name, bool isLeftSide)
    {
        Client = client;
        Name = name;
        IsLeftSide = isLeftSide;
        ownerUnlockedUnitId = client.OwnedCharacterIds;
    }

    public bool IsMyTurn(PlayerRef @ref)
    {
        return Client.PlayerRef == @ref;
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

    public bool ApplyUnitIdPicked(int unitId)
    {
        return pickedUnitIds.Add(unitId);
    }

    public bool ApplyUnitIdBanned(int unitId)
    {
        return bannedUnitIds.Add(unitId);
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
        if (index < 0 || index >= pickedUnitIds.Count)
        {
            return false;
        }

        unitId = pickedUnitIds.ElementAt(index);
        return true;
    }

    public bool HasReachedDeployLimit(int battleLimit)
    {
        bool isPlayerHaveUnitEqualBattleLimit = ownerUnlockedUnitId.Count >= battleLimit;

        if (!isPlayerHaveUnitEqualBattleLimit)
            battleLimit = ownerUnlockedUnitId.Count;

        bool isReachLimit = pickedUnitIds.Count >= battleLimit;
        return isReachLimit;
    }

    public void HandleBanPickTurnStart(Battle battle, bool hasBanPhase)
    {

        bool banComplete = !hasBanPhase || bannedUnitIds.Count >= battle.CurrentTurnCount * MaxUnitBanPerTurn;

        bool pickComplete = pickedUnitIds.Count >= battle.CurrentTurnCount || ownerUnlockedUnitId.Count < battle.CurrentTurnCount;

        if (banComplete && pickComplete)
        {
            battle.HandlePlayerTurnDone();
        }
        else
        {
            ServerNetwork.Instance.SendToClients(
                Service.SendPlayerTurnToDeploy(battle.CurrentTurnCount, Client.PlayerRef.PlayerId),
                battle.PlayerClients);
        }
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
            unit.ExecuteSkillBuff();
            if (unit.Data.IsYuanUser)
            {
                unit.PlusSkillPoint(1);
            }
            unit.TriggerPassives(PassiveTriggerType.TurnStart, new TurnStartEvent(unit), battle.CreateBattleContext(this, unit));
        }
    }

    private void TriggerYuanBuff(Unit unit)
    {
        YuanBuff yuanBuff = yuanPressureSystem.Buff;
        if (yuanBuff.HealApply < 0)
        {
            unit.TakeDamage(-yuanBuff.HealApply);
            return;
        }
        unit.PlusHp(yuanBuff.HealApply);
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
        TriggerYuanBuff(unit);
        unit.TriggerPassives(PassiveTriggerType.ActionPerformed, new ActionPerformedEvent(unit), battle.CreateBattleContext(this, unit));
        ServerNetwork.Instance.SendToClient(Client, Service.PlayerResourceInfo(ApSystem.Current, YuanPressureSystem.Current));
        ServerNetwork.Instance.SendToClients(Service.UnitMove(Client.PlayerRef.PlayerId, unit.Id, paths), battle.PlayerClients);
    }

    public void SetUnitPlaced(Battle battle, PlaceUnit placeUnit)
    {
        if (!pickedUnitIds.Contains(placeUnit.UnitId))
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
            enemy.ApplySkillBuff(selectedSkill.Buffs);
            enemy.ApplySkillDebuff(selectedSkill.Debuffs);
            affectedUnits.Add(enemy);
        }
        lastUnitUsedSkill = unit;
        listUnitHavePendingDamage.Clear();
        listUnitHavePendingDamage.AddRange(affectedUnits);
        ServerNetwork.Instance.SendToClient(Client, Service.YuanPressureUpdate(yuanPressureSystem.Current));
        ServerNetwork.Instance.SendToClients(
           Service.UseSkillResult(
               Client.PlayerRef.PlayerId,
               unit.Id,
               yuanPressureSystem.IsYuanMode,
               selectedSkill.AnimationTrigger.ToString(),
               request.TargetCell),
               battle.PlayerClients
           );
    }

    public void ExecuteTurnDone()
    {
        foreach (var unit in unitsByCharId.Values)
        {
            if (!unit.IsAlive)
                continue;

            unit.ExecuteSkillDebuff();
        }
    }

    public void HandleOnFrameHit(Battle battle)
    {
        foreach (Unit unit in ListUnitHavePendingDamage)
        {
            unit.ApplyPendingDamage();
        }
        ListUnitHavePendingDamage.Clear();

        yuanPressureSystem.AdjustValue(selectedSkill.YuanLiCost);
        TriggerYuanBuff(lastUnitUsedSkill);
        lastUnitUsedSkill.TriggerPassives(PassiveTriggerType.ActionPerformed, new ActionPerformedEvent(lastUnitUsedSkill), battle.CreateBattleContext(this, lastUnitUsedSkill));
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
