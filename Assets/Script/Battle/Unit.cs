using System;
using System.Collections.Generic;
using System.Linq;
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
    public Vector3Int FacingDirection;
    public CharacterDataJsonData Data;
    public Vector3Int CurrentGridPosition;
    public IReadOnlyList<int> EquippedBasicAttackSkillIds => equippedBasicAttackSkillIds;
    public IReadOnlyList<int> EquippedYuanSkillIds => equippedYuanSkillIds;
    public IReadOnlyList<int> EquippedPassiveIds => equippedPassiveIds;

    private readonly List<int> equippedBasicAttackSkillIds = new(MaxBasicAttackSkills);
    private readonly List<int> equippedYuanSkillIds = new(MaxYuanSkills);
    private readonly List<int> equippedPassiveIds = new(MaxPassives);

    private readonly Dictionary<int, BasicAttackSkill> basicAttackByIds = new(MaxBasicAttackSkills);
    private readonly Dictionary<int, YuanSkill> yuanSkillByIds = new(MaxYuanSkills);
    private readonly Dictionary<int, Passive> PassiveByIds = new(MaxPassives);

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
        SkillPoint = source.SkillPoint;
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
        SkillPoint = Data.InitialSkillPoint;
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
}

public enum SkillLoadoutType
{
    None = -1,
    BasicAttack = 0,
    YuanSkill = 1,
    Passive = 2
}
