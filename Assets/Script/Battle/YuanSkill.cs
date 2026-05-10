using System;
using UnityEngine;

public class YuanSkill : Skill
{
    private readonly int actionPointCost;
    private readonly int yuanLiCost;
    private readonly int skillPointCost;
    private readonly SkillPattern skillPattern;
    private readonly UnitAnimationState animationTrigger;

    public override int ActionPointCost => actionPointCost;
    public override int YuanLiCost => yuanLiCost;
    public override int SkillPointCost => skillPointCost;
    public override SkillPattern SkillPattern => skillPattern;
    public override UnitAnimationState AnimationTrigger => animationTrigger;

    public YuanSkill(YuanSkillJsonData data)
    {
        ApplyBaseData(data);
        actionPointCost = data.ActionPointCost;
        yuanLiCost = data.YuanLiCost;
        skillPointCost = data.SkillPointCost;
        skillPattern = SkillPattern.FromJson(data.SkillPattern);
        animationTrigger = string.IsNullOrWhiteSpace(data.AnimationTrigger)
            ? UnitAnimationState.Attack_1
            : Enum.Parse<UnitAnimationState>(data.AnimationTrigger);
    }

    public static YuanSkill FromJson(YuanSkillJsonData yuanSkillJson)
    {
        return yuanSkillJson == null ? null : new YuanSkill(yuanSkillJson);
    }
}
