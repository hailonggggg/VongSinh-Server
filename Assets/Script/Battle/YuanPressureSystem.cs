using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class YuanPressureSystem
{
    public int Current => current;
    public int Max => max;
    public bool IsDangerLevel => current >= 6;
    public bool IsYuanMode { get; private set; }
    public YuanBuff Buff
    {
        get
        {
            if (current > 10) return yuanBuffs[ThreatLevel.Dissonance];
            if (current > 5) return yuanBuffs[ThreatLevel.Resonance];
            return yuanBuffs[ThreatLevel.Sustenance];
        }
    }

    private readonly int POINT_CHANGE_MODE = 6;
    private readonly int max;
    private int current;

    private readonly Dictionary<ThreatLevel, YuanBuff> yuanBuffs = new Dictionary<ThreatLevel, YuanBuff>()
    {
        {ThreatLevel.Sustenance, new YuanBuff{HealApply = 2}},
        {ThreatLevel.Resonance, new YuanBuff{DamageBuff = 1,}},
        {ThreatLevel.Dissonance, new YuanBuff{HealApply = -6,DamageBuff = 3,CritRateBuff = 15}}
    };

    public YuanPressureSystem(int initialValue = 0, int maxValue = 15)
    {
        max = Mathf.Max(1, maxValue);
        current = Mathf.Clamp(initialValue, 0, max);
    }

    public void AdjustValue(int amount)
    {
        int previousPoint = current;
        current = Mathf.Clamp(current + amount, 0, max);

        if (previousPoint < POINT_CHANGE_MODE && current >= POINT_CHANGE_MODE)
        {
            IsYuanMode = true;
        }
        else if (previousPoint >= POINT_CHANGE_MODE && current < POINT_CHANGE_MODE)
        {
            IsYuanMode = false;
        }
    }

    public bool TryConsume(int cost)
    {
        if (cost < 0 || current < cost)
        {
            return false;
        }

        current -= cost;
        return true;
    }
}

public class YuanBuff
{
    public int HealApply = 0;
    public int DamageBuff = 0;
    public int CritRateBuff = 0;
}

