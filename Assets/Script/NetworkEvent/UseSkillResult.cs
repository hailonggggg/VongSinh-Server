using System;
using UnityEngine;

[Serializable]
public class UseSkillResult
{
    public int PlayerId;
    public int UnitId;
    public bool IsYuanMode;
    public string AnimationTrigger;
    public Vector3Int TargetCell;
}
