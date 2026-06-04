using System;
using UnityEngine;

public class StatusEffect
{
    public int Id;
    public SkillEffectType EffectType;

    public static StatusEffect FromJson(StatusEffectJsonData data)
    {
        return new StatusEffect
        {
            Id = data.Id,
            EffectType = Enum.Parse<SkillEffectType>(data.EffectType)
        };
    }
}
