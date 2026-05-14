using System;
using UnityEngine;

[Serializable]
public class UseSkillRequest
{
    public int UnitId;
    public int SkillId;
    public int SkillType;
    public Vector3Int TargetCell;
}
