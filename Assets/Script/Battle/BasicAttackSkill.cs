using System;
using System.Collections.Generic;
using UnityEngine;

public class BasicAttackSkill : Skill
{
    public bool HaveYuanMode;
    public bool IsYuanMode = false;
    public SkillInfo NormalInfo;
    public SkillInfo YuanInfo;

    public override int ActionPointCost => GetActiveSkillInfo()?.ActionPointCost ?? 0;
    public override int YuanLiCost => GetActiveSkillInfo()?.YuanLiCost ?? 0;
    public override int SkillPointCost => GetActiveSkillInfo()?.SkillPointCost ?? 0;
    public override SkillPattern SkillPattern => GetActiveSkillInfo()?.SkillPattern;
    public override UnitAnimationState AnimationTrigger => GetActiveSkillInfo()?.AnimationTrigger ?? UnitAnimationState.Attack_1;

    public BasicAttackSkill(BasicAttackSkillJsonData basicAttackData) : base()
    {
        ApplyBaseData(basicAttackData);
        HaveYuanMode = basicAttackData.HaveYuanMode;
        IsYuanMode = basicAttackData.IsYuanMode;
        NormalInfo = SkillInfo.FromJson(basicAttackData.NormalInfo);
        YuanInfo = SkillInfo.FromJson(basicAttackData.YuanInfo);
    }

    public BasicAttackSkill(BasicAttackSkill source) : base(source)
    {
        HaveYuanMode = source.HaveYuanMode;
        IsYuanMode = source.IsYuanMode;
        NormalInfo = source.NormalInfo;
        YuanInfo = source.YuanInfo;
    }


    public static BasicAttackSkill FromJson(BasicAttackSkillJsonData skillData)
    {
        return skillData == null ? null : new BasicAttackSkill(skillData);
    }

    private SkillInfo GetActiveSkillInfo()
    {
        if (HaveYuanMode && IsYuanMode && YuanInfo != null)
        {
            return YuanInfo;
        }

        return NormalInfo;
    }

    public override List<SkillTileData> GetAffectedTileData(Vector3Int previewDirection)
    {
        SkillInfo activeInfo = GetActiveSkillInfo();
        if (activeInfo == null || activeInfo.SkillPattern == null)
        {
            return new List<SkillTileData>();
        }

        return activeInfo.SkillPattern.GetAffectedTileData(previewDirection);
    }

    public override Skill Clone()
    {
        return new BasicAttackSkill(this);
    }

    public void SetStats(CharacterBasicAttack basicAttackStats)
    {
        SkillName = basicAttackStats.Name;
        Description = basicAttackStats.Description;
        Damage = basicAttackStats.Damage;
        CritRate = basicAttackStats.CritRate;
        if (basicAttackStats.NormalInfo != null && NormalInfo != null)
        {
            NormalInfo.ActionPointCost = basicAttackStats.NormalInfo.ActionPointCost;
            NormalInfo.SkillPointCost = basicAttackStats.NormalInfo.SkillPointCost;
            NormalInfo.YuanLiCost = basicAttackStats.NormalInfo.YuanLiCost;
        }
        if (basicAttackStats.YuanInfo != null && YuanInfo != null)
        {
            YuanInfo.ActionPointCost = basicAttackStats.YuanInfo.ActionPointCost;
            YuanInfo.SkillPointCost = basicAttackStats.YuanInfo.SkillPointCost;
            YuanInfo.YuanLiCost = basicAttackStats.YuanInfo.YuanLiCost;
        }
    }
}

public class SkillInfo
{
    public int ActionPointCost = 1;
    public int YuanLiCost = 1;
    public int SkillPointCost = 0;
    public UnitAnimationState AnimationTrigger = UnitAnimationState.Attack_1;
    public SkillPattern SkillPattern;

    public static SkillInfo FromJson(SkillInfoJsonData data)
    {
        return new SkillInfo
        {
            ActionPointCost = data.ActionPointCost,
            YuanLiCost = data.YuanLiCost,
            SkillPointCost = data.SkillPointCost,
            AnimationTrigger = Enum.Parse<UnitAnimationState>(data.AnimationTrigger),
            SkillPattern = SkillPattern.FromJson(data.SkillPattern)
        };
    }
}
