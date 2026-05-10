using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Passive
{
    public int Id;
    public string AssetName;
    public string PassiveName;
    public string Description;
    public bool IsUnlocked;
    public List<PassiveCondition> Conditions;
    public List<PassiveEffect> Effects;

    public static Passive FromJson(PassiveJsonData data)
    {
        return new Passive
        {
            Id = data.Id,
            PassiveName = data.PassiveName,
            Description = data.Description,
            IsUnlocked = data.IsUnlocked,
            Conditions = data.Conditions.Select(x => new PassiveCondition
            {
                Type = Enum.Parse<PassiveConditionType>(x.Type),
                EnemyGetHitRequired = x.EnemyGetHitRequired,
                ThreatLevel = Enum.Parse<ThreatLevel>(x.ThreadLevel)
            }).ToList(),
            Effects = data.Effects.Select(x => new PassiveEffect
            {
                Type = Enum.Parse<PassiveEffectType>(x.Type),
                Amount = x.Amount,
                IncludeOwner = x.IncludeOwner,
                Range = x.Range
            }).ToList(),
        };
    }
}
