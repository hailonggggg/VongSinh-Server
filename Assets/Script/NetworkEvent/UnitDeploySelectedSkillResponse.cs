using System;
using UnityEngine;

[Serializable]
public class UnitDeploySelectedSkillResponse
{
    public int PlayerId;
    public int UnitId;
    public int SkillId;
    public int Type;
    public bool IsChecked;
}
