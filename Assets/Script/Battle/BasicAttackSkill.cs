using System;
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

    public BasicAttackSkill(BasicAttackSkillJsonData basicAttackData)
    {
        ApplyBaseData(basicAttackData);
        HaveYuanMode = basicAttackData.HaveYuanMode;
        IsYuanMode = basicAttackData.IsYuanMode;
        NormalInfo = SkillInfo.FromJson(basicAttackData.NormalInfo);
        YuanInfo = SkillInfo.FromJson(basicAttackData.YuanInfo);
    }
    public static BasicAttackSkill FromJson(BasicAttackSkillJsonData skillData)
    {
        return skillData == null ? null : new BasicAttackSkill(skillData);
    }

    private SkillInfo GetActiveSkillInfo()
    {
        if (IsYuanMode && YuanInfo != null)
        {
            return YuanInfo;
        }

        return NormalInfo ?? YuanInfo;
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

        };
    }
}
