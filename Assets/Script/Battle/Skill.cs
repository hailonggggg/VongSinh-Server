using System;
using UnityEngine;

public class Skill
{
    public int Id;
    public string AssetName;
    public string SkillType;
    public string SkillName = "New Skill";
    public string Description = "";
    public bool IsDirectional;
    public bool IsUnlocked = false;
    public int Damage;
    public int CritRate;
    public SkillOrigin SkillOrigin = SkillOrigin.CharacterPosition;
    public int MoveRange = 0;
    public int CooldownTurns;


    public virtual int ActionPointCost { get; }
    public virtual int YuanLiCost { get; }
    public virtual int SkillPointCost { get; }
    public virtual SkillPattern SkillPattern { get; }
    public virtual UnitAnimationState AnimationTrigger { get; }

    protected void ApplyBaseData(SkillJsonData data)
    {
        if (data == null)
        {
            return;
        }

        Id = data.Id;
        AssetName = data.AssetName;
        SkillType = data.SkillType;
        SkillName = data.SkillName;
        Description = data.Description;
        IsDirectional = data.IsDirectional;
        IsUnlocked = data.IsUnlocked;
        Damage = data.Damage;
        CritRate = data.CritRate;
        SkillOrigin = string.IsNullOrWhiteSpace(data.SkillOrigin)
            ? SkillOrigin.CharacterPosition
            : Enum.Parse<SkillOrigin>(data.SkillOrigin);
        MoveRange = data.MoveRange;
        CooldownTurns = data.CooldownTurns;
    }
}


public enum SkillOrigin
{
    CharacterPosition,
    AdjacentTile,
    TargetPosition,
    SelectedTile
}
