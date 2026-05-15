using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Unit
{
    public const int MaxBasicAttackSkills = 2;
    public const int MaxYuanSkills = 2;
    public const int MaxPassives = 2;
    public int Id;
    public int MoveRange;
    public int CurrentHealth;
    public int SkillPoint;
    public int MaxHealth;
    public bool IsAlive => CurrentHealth > 0;
    public int PendingDamage;
    public Vector3Int FacingDirection = Vector3Int.zero;
    public CharacterDataJsonData Data;
    public Vector3Int CurrentGridPosition;
    public BattlePlayer Owner => owner;
    public IReadOnlyList<int> EquippedBasicAttackSkillIds => equippedBasicAttackSkillIds;
    public IReadOnlyList<int> EquippedYuanSkillIds => equippedYuanSkillIds;
    public IReadOnlyList<int> EquippedPassiveIds => equippedPassiveIds;
    public IDictionary<int, BasicAttackSkill> BasicAttackByIds => basicAttackByIds;
    public IDictionary<int, YuanSkill> YuanSkillByIds => yuanSkillByIds;
    public IDictionary<int, Passive> PassiveByIds => passiveByIds;


    private readonly List<int> equippedBasicAttackSkillIds = new(MaxBasicAttackSkills);
    private readonly List<int> equippedYuanSkillIds = new(MaxYuanSkills);
    private readonly List<int> equippedPassiveIds = new(MaxPassives);

    private readonly Dictionary<int, BasicAttackSkill> basicAttackByIds = new(MaxBasicAttackSkills);
    private readonly Dictionary<int, YuanSkill> yuanSkillByIds = new(MaxYuanSkills);
    private readonly Dictionary<int, Passive> passiveByIds = new(MaxPassives);

    private BattlePlayer owner;
    private Battle battle;

    public Unit(CharacterDataJsonData data, Vector3Int position = default)
    {
        Data = data;
        CurrentGridPosition = position;
        InitializeData();
        InitializeLoadout();
    }

    private Unit(Unit source)
    {
        Id = source.Id;
        MoveRange = source.MoveRange;
        CurrentHealth = source.CurrentHealth;
        SkillPoint = 0;
        MaxHealth = source.MaxHealth;
        FacingDirection = source.FacingDirection;
        Data = source.Data;
        CurrentGridPosition = source.CurrentGridPosition;

        equippedBasicAttackSkillIds.AddRange(source.equippedBasicAttackSkillIds);
        equippedYuanSkillIds.AddRange(source.equippedYuanSkillIds);
        equippedPassiveIds.AddRange(source.equippedPassiveIds);
    }

    private void InitializeData()
    {
        Id = Data.Id;
        MoveRange = Data.MoveRange;
        CurrentHealth = Data.MaxHP;
        MaxHealth = Data.MaxHP;
        SkillPoint = 0;
    }

    public void SetOwner(BattlePlayer battlePlayer, Battle battle)
    {
        owner = battlePlayer;
        this.battle = battle;
    }

    public bool TryAssignSkill(int skillId, SkillLoadoutType loadoutType)
    {
        return loadoutType switch
        {
            SkillLoadoutType.BasicAttack => TryAssignToList(equippedBasicAttackSkillIds, skillId, MaxBasicAttackSkills),
            SkillLoadoutType.YuanSkill => TryAssignToList(equippedYuanSkillIds, skillId, MaxYuanSkills),
            SkillLoadoutType.Passive => TryAssignToList(equippedPassiveIds, skillId, MaxPassives),
            _ => false
        };
    }

    public List<int> GetListSkillLoadout(SkillLoadoutType loadoutType)
    {
        return loadoutType switch
        {
            SkillLoadoutType.BasicAttack => equippedBasicAttackSkillIds,
            SkillLoadoutType.YuanSkill => equippedYuanSkillIds,
            SkillLoadoutType.Passive => equippedPassiveIds,
            _ => new List<int>()
        };
    }

    public bool RemoveSkillEquipped(int skillId, SkillLoadoutType loadoutType)
    {
        return loadoutType switch
        {
            SkillLoadoutType.BasicAttack => equippedBasicAttackSkillIds.Remove(skillId),
            SkillLoadoutType.YuanSkill => equippedYuanSkillIds.Remove(skillId),
            SkillLoadoutType.Passive => equippedPassiveIds.Remove(skillId),
            _ => false
        };
    }

    public Unit Clone()
    {
        return new Unit(this);
    }

    private void InitializeLoadout()
    {
        if (Data?.basicAttackSkillIds != null)
        {
            equippedBasicAttackSkillIds.AddRange(Data.basicAttackSkillIds.Take(MaxBasicAttackSkills));
        }

        if (Data?.yuanSkillIds != null)
        {
            equippedYuanSkillIds.AddRange(Data.yuanSkillIds.Take(MaxYuanSkills));
        }

        if (Data?.PassiveIds != null)
        {
            equippedPassiveIds.AddRange(Data.PassiveIds.Take(MaxPassives));
        }
    }

    public void AddAllSkill()
    {
        foreach (var item in equippedBasicAttackSkillIds)
        {
            if (!basicAttackByIds.ContainsKey(item))
            {
                basicAttackByIds[item] = Master.Instance.BasicAttackSkillsById[item].Clone() as BasicAttackSkill;
            }
        }

        foreach (var item in equippedYuanSkillIds)
        {
            if (!yuanSkillByIds.ContainsKey(item))
            {
                yuanSkillByIds[item] = Master.Instance.YuanSkillsById[item].Clone() as YuanSkill;
            }
        }

        foreach (var item in equippedPassiveIds)
        {
            if (!passiveByIds.ContainsKey(item))
            {
                passiveByIds[item] = Master.Instance.PassiveByIds[item].Clone() as Passive;
            }
        }
    }

    private static bool TryAssignToList(List<int> targetList, int skillId, int maxCount)
    {
        if (targetList.Contains(skillId))
        {
            return true;
        }

        if (targetList.Count >= maxCount)
        {
            return false;
        }

        targetList.Add(skillId);
        return true;
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(CurrentHealth - damage, 0);
        ServerNetwork.Instance.SendToClients(
            Service.ReceiveDamage(owner.Client.PlayerRef.PlayerId, Id, damage, CurrentHealth)
            , battle.PlayerClients);
        if (CurrentHealth <= 0)
        {
            ServerNetwork.Instance.SendToClients(
                Service.UnitDeathResult(owner.Client.PlayerRef.PlayerId, Id)
                , battle.PlayerClients);
        }
    }

    public void ApplyPendingDamage()
    {
        TakeDamage(PendingDamage);
        PendingDamage = 0;
    }

    public void PlusSkillPoint(int amount)
    {
        int lastSkillPoint = SkillPoint;
        lastSkillPoint += amount;
        SetSkillpoint(lastSkillPoint);
    }

    public void SetSkillpoint(int amount)
    {
        SkillPoint = Math.Max(amount, 0);
        ServerNetwork.Instance.SendToClients(
            Service.UnitInfoResult(owner.Client.PlayerRef.PlayerId, Id, SkillPoint, CurrentHealth),
            battle.PlayerClients);
    }

    public void TriggerPassives(PassiveTriggerType trigger, IPassiveEvent passiveEvent, BattleContext context)
    {
        PassiveContext passiveContext = new()
        {
            Trigger = trigger,
            PassiveEvent = passiveEvent,
            BattleContext = context
        };

        foreach (var passive in passiveByIds.Values)
        {
            if (passive == null || passive.TriggerType != trigger)
                continue;

            passive.Execute(this, passiveContext);
        }
    }

    public void SetHealth(int amount)
    {
        CurrentHealth = Math.Clamp(amount, 0, MaxHealth);
        ServerNetwork.Instance.SendToClients(
            Service.UnitInfoResult(owner.Client.PlayerRef.PlayerId, Id, SkillPoint, CurrentHealth),
            battle.PlayerClients);
    }

    public void PlusHp(int amount)
    {
        int lastHp = CurrentHealth;
        lastHp += amount;
        int healAmount = Math.Min(lastHp, MaxHealth) - CurrentHealth;
        if (healAmount <= 0) return;

        ServerNetwork.Instance.SendToClients(
           Service.UnitHealResult(owner.Client.PlayerRef.PlayerId, Id, healAmount, CurrentHealth), battle.PlayerClients);
        SetHealth(lastHp);
    }

    public bool TryConsumeSkillPoint(int yuanLiCost)
    {
        if (yuanLiCost < 0 || SkillPoint < yuanLiCost)
        {
            return false;
        }

        SkillPoint -= yuanLiCost;
        ServerNetwork.Instance.SendToClients(
            Service.UnitInfoResult(owner.Client.PlayerRef.PlayerId, Id, SkillPoint, CurrentHealth), battle.PlayerClients);
        return true;
    }
}

public enum SkillLoadoutType
{
    None = -1,
    BasicAttack = 0,
    YuanSkill = 1,
    Passive = 2
}
