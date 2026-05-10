using System;
using UnityEngine;

[Serializable]
public class YuanSkillJsonData : SkillJsonData
{
    public int ActionPointCost;
    public int YuanLiCost;
    public int SkillPointCost;
    public string AnimationTrigger;
    public SkillPatternJsonData SkillPattern;
}
