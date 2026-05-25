using System;
using UnityEngine;

[Serializable]
public class CharacterBasicAttack
{
    public int CharacterAttackId;
    public string Name;
    public string Description;
    public int Damage;
    public int CritDmg;
    public int CritRate;
    public CharacterAttackNormalInfo NormalInfo;
    public CharacterAttackYuanInfo YuanInfo;
    public int LockedState;
}

[Serializable]
public class CharacterAttackNormalInfo
{
    public int ActionPointCost = 1;
    public int YuanLiCost = 1;
    public int SkillPointCost = 0;
}

[Serializable]
public class CharacterAttackYuanInfo
{
    public int ActionPointCost = 1;
    public int YuanLiCost = 1;
    public int SkillPointCost = 0;
}

