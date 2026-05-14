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
    public PassiveTriggerType TriggerType;
    public List<PassiveCondition> Conditions;
    public List<PassiveEffect> Effects;
    public Passive()
    {
    }
    public Passive(Passive source)
    {
        Id = source.Id;
        AssetName = source.AssetName;
        PassiveName = source.PassiveName;
        Description = source.Description;
        IsUnlocked = source.IsUnlocked;
        TriggerType = source.TriggerType;
        Conditions = source.Conditions.Select(x => new PassiveCondition
        {
            Type = x.Type,
            EnemyGetHitRequired = x.EnemyGetHitRequired,
            ThreatLevel = x.ThreatLevel
        }).ToList();
        Effects = source.Effects.Select(x => new PassiveEffect
        {
            Type = x.Type,
            Amount = x.Amount,
            IncludeOwner = x.IncludeOwner,
            Range = x.Range
        }).ToList();
    }

    public static Passive FromJson(PassiveJsonData data)
    {
        return new Passive
        {
            Id = data.Id,
            PassiveName = data.PassiveName,
            Description = data.Description,
            IsUnlocked = data.IsUnlocked,
            TriggerType = Enum.Parse<PassiveTriggerType>(data.TriggerType),
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

    public Passive Clone()
    {
        return new Passive(this);
    }

    public void Execute(Unit owner, PassiveContext context)
    {
        if (Conditions.All(c => c.Evaluated(owner, context)))
        {
            foreach (var eff in Effects)
            {
                eff.Execute(owner, context);
            }
        }
    }
}
