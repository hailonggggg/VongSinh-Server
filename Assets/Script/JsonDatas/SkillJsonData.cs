using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillJsonData
{
    public int Id;
    public string AssetName;
    public string SkillType;
    public string SkillName;
    public string Description;
    public bool IsDirectional;
    public bool IsUnlocked;
    public int Damage;
    public int CritRate;
    public string SkillOrigin;
    public int MoveRange;
    public int CooldownTurns;
    public List<SkillOptionJsonData> BuffOptions;
    public List<SkillOptionJsonData> DebuffOptions;
    public List<SkillStageJsonData> Stages;
}
