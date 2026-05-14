using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PassiveJsonData
{
    public int Id;
    public string AssetName;
    public string PassiveName;
    public string Description;
    public bool IsUnlocked;
    public string TriggerType;
    public List<PassiveConditionJsonData> Conditions;
    public List<PassiveEffectJsonData> Effects;
}
