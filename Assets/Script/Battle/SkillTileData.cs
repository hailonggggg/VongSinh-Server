using System;
using UnityEngine;


[Serializable]
public struct SkillTileData
{
    public Vector3Int offset;
    public bool IsTargetCell;
    public SkillTileColor tileColor;
    [Tooltip("He so sat thuong: 1 = 100%, 0.5 = 50%...")]
    public float damageMultiplier;
}
