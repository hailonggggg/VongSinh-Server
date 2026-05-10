using System;
using UnityEngine;

[Serializable]
public class BasicAttackSkillJsonData : SkillJsonData
{
    public bool HaveYuanMode;
    public bool IsYuanMode;
    public SkillInfoJsonData NormalInfo;
    public SkillInfoJsonData YuanInfo;
}
