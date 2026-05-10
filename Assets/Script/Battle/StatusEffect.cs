using System;
using UnityEngine;

public class StatusEffect
{
    public int Id;
    public EffectType EffectType;

    public static StatusEffect FromJson(StatusEffectJsonData data)
    {
        return new StatusEffect
        {
            Id = data.Id,
            EffectType = Enum.Parse<EffectType>(data.EffectType)
        };
    }
}

public enum EffectType
{
    None,
    Bleed
}