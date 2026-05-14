using System;
using UnityEngine;

[Serializable]
public class ReceiveDamageResult
{
    public int PlayerId;
    public int UnitId;
    public int DamageAmount;
    public int RemainingHealth;
}

